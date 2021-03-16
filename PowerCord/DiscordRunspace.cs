using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.PowerShell;
using Microsoft.PowerShell.Commands;
using Microsoft.PowerShell.Commands.Internal.Format;
using PowerCord.Converters;

namespace PowerCord
{
    public class DiscordRunspace
    {
        private record QueueItem(DiscordMessage Message, string Content);
        private ConcurrentQueue<QueueItem> _queue;

        private DiscordClient _client;
        private DiscordGuild _guild;
        private DiscordEmoji _noResponseEmoji;
        private DiscordEmoji _timeoutEmoji;
        private ILogger<DiscordRunspace> _logger;

        private Runspace _runspace;
        private Task _queueTask;
        private int _timeout = 10;
        private string _globalDirectory;
        private bool _reset;

        public DiscordRunspace(
            DiscordClient client,
            DiscordGuild guild,
            ILogger<DiscordRunspace> logger,
            IConfiguration configuration,
            IHostEnvironment hostingEnvironment)
        {
            _client = client;
            _guild = guild;
            _logger = logger;

            _noResponseEmoji = DiscordEmoji.FromName(client, configuration["PowerCord:NoResponseEmoji"]);
            _timeoutEmoji = DiscordEmoji.FromName(client, configuration["PowerCord:TimeoutEmoji"]);
            _timeout = configuration.GetValue<int>("PowerCord:Timeout");
            _globalDirectory = Path.Combine(hostingEnvironment.ContentRootPath, configuration["PowerCord:GlobalDirectoryName"]);

            _queue = new ConcurrentQueue<QueueItem>();
        }

        public Task ExecuteStringAsync(DiscordMessage message, string content)
        {
            _queue.Enqueue(new QueueItem(message, content));

            if (_queueTask == null || _queueTask.IsCompleted)
                _queueTask = Task.Run(ProcessQueueAsync);

            return Task.CompletedTask;
        }

        private async Task ProcessQueueAsync()
        {
            while (_queue.TryDequeue(out var item))
            {
                if (_runspace == null)
                    InitRunspace();

                item.Deconstruct(out var message, out var content);

                var sanatisedContent = content.Replace("\r", "\\r").Replace("\n", "\\n");

                Exception exception = null;
                try
                {
                    var context = new PSDiscordCmdletContext(message, message.Author, message.Author as DiscordMember, message.Channel, message.Channel.Guild, _client, this);
                    _runspace.SessionStateProxy.PSVariable.Remove("ctx", true);
                    _runspace.SessionStateProxy.PSVariable.Set(new PSVariable("ctx", context, ScopedItemOptions.ReadOnly));

                    using (var pipeline = _runspace.CreatePipeline(content))
                    {
                        pipeline.RunWithTimeout(TimeSpan.FromSeconds(_timeout), out var token, out exception, out var output);

                        var ret = await _runspace.GetOutputStringAsync(pipeline, context, output, exception);
                        if (!string.IsNullOrWhiteSpace(ret))
                            await message.Channel.SendChunkedMessageAsync(ret);
                        else if (token.IsCancellationRequested)
                            await message.CreateReactionAsync(_timeoutEmoji);
                        else
                            await message.CreateReactionAsync(_noResponseEmoji);
                    }
                }
                catch (Exception ex1)
                {
                    // so this should log any discord errors and prevent crashes
                    exception = ex1;
                }

                if (exception != null)
                {
                    _logger.LogError(exception, "{Command} in {Channel} failed to execute!", sanatisedContent, message.Channel);
                }
                else
                {
                    _logger.LogInformation("{User} successfully executed \"{Command}\" in {Channel}!", message.Author, sanatisedContent, message.Channel);
                }

                if (_reset)
                {
                    _logger.LogInformation("{User} requested Runspace reset.", message.Author);
                    _reset = false;

                    _runspace.Close();
                    _runspace.Dispose();
                    _runspace = null;
                }
            }
        }

        private void InitRunspace()
        {
            var initialState = InitialSessionState.CreateDefault();
            initialState.ThrowOnRunspaceOpenError = true;
            initialState.LanguageMode = PSLanguageMode.ConstrainedLanguage;
            initialState.ThreadOptions = PSThreadOptions.UseNewThread;
            initialState.EnvironmentVariables.Clear();
            initialState.Commands.Clear();

            // user drives are funky on non-windows platforms
            if (OperatingSystem.IsWindows())
            {
                initialState.UserDriveEnabled = true;
                initialState.UserDriveMaximumSize = 8_000_000;
                initialState.UserDriveUserName = _guild.Id.ToString();
                initialState.GlobalDrivePath = _globalDirectory;

#if DEBUG
                initialState.ExecutionPolicy = ExecutionPolicy.Unrestricted;
#else
                initialState.ExecutionPolicy = ExecutionPolicy.AllSigned;
#endif
            }

            initialState.Providers.Remove("Registry", typeof(SessionStateProviderEntry));
            initialState.Providers.Remove("Environment", typeof(SessionStateProviderEntry));
            initialState.Providers.Remove("Certificate", typeof(SessionStateProviderEntry));
            initialState.Providers.Remove("WSMan", typeof(SessionStateProviderEntry));

            initialState.AddTypeConverter<DiscordUser, DiscordUserConverter>();
            initialState.AddTypeConverter<DiscordChannel, DiscordChannelConverter>();
            initialState.AddTypeConverter<DiscordMessage, DiscordMessageConverter>();

            initialState.Variables.Add(new SessionStateVariableEntry(SpecialVariables.PSModuleAutoLoading, PSModuleAutoLoadingPreference.None, string.Empty, ScopedItemOptions.Constant));
            initialState.EnvironmentVariables.Add(new SessionStateVariableEntry("PSModulePath", "Global:\\Modules", string.Empty, ScopedItemOptions.Constant));

            Utilities.RegisterTypes(initialState, Enumerable.Empty<Type>());

            initialState.StartupBlocks.Add(Utilities.StartupScript);

            // var modulesBasepath = Path.Combine(Directory.GetCurrentDirectory(), "Global", "Modules");
            // initialState.ImportPSModulesFromPath("C:\\Modules");

            _runspace = RunspaceFactory.CreateRunspace(new DiscordHost(PSObject.AsPSObject(_guild)), initialState);
            _runspace.Open();

            _runspace.SessionStateProxy.Scripts.Add("*");
            TryImportModules();
        }

        private void TryImportModules()
        {
            var context = _runspace.ExecutionContext;
            Runspace.DefaultRunspace = _runspace;

            foreach (var fileName in Directory.GetFiles(_globalDirectory, "*.psd1", SearchOption.AllDirectories))
            {
                var newName = fileName.Replace(_globalDirectory, "Global:");
                _logger.LogDebug("Loading module from {Path}", newName);

                try
                {
                    using var processor = new PipelineProcessor();
                    using var importModuleCommand = new CommandProcessor(new CmdletInfo("import-module", typeof(ImportModuleCommand), null, null, context), context);
                    importModuleCommand.AddParameter(CommandParameterInternal.CreateParameterWithArgument(null, "Name", null, null, new[] { newName }, false));
                    processor.Add(importModuleCommand);
                    processor.StartStepping(false);
                    processor.DoComplete();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load module at {Path}", newName);
                }
            }
        }

        internal void SetResetFlag()
        {
            _reset = true;
        }
    }
}

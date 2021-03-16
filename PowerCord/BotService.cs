using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;

namespace PowerCord
{
    public class BotService : IHostedService
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _services;
        private readonly ILogger<BotService> _logger;
        private readonly DiscordClient _client;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly Dictionary<ulong, DiscordRunspace> _runspaces;
        private readonly string _prefix;

        public BotService(
            IConfiguration configuration,
            IServiceProvider services,
            IHostEnvironment hostEnvironment,
            ILogger<BotService> logger,
            ILoggerFactory loggerFactory,
            DiscordClient client)
        {
            _configuration = configuration;
            _services = services;
            _logger = logger;
            _client = client;
            _loggerFactory = loggerFactory;
            _hostEnvironment = hostEnvironment;
            _runspaces = new Dictionary<ulong, DiscordRunspace>();
            _prefix = configuration["Discord:Prefix"];
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var ignoreConfigIssues = _configuration.GetValue<bool>("PowerCord:IgnoreConfigurationErrors");
            var globalDirectory = _configuration["PowerCord:GlobalDirectoryName"];

            if (TestPath(Path.Combine(Directory.GetCurrentDirectory(), "test.tmp")))
            {
                HandleConfigurationProblem("The current directory is writeable", ignoreConfigIssues);
            }

            if (TestPath(Path.Combine(_hostEnvironment.ContentRootPath, "test.tmp")))
            {
                HandleConfigurationProblem("The content root is writeable", ignoreConfigIssues);
            }

            if (TestPath(Path.Combine(_hostEnvironment.ContentRootPath, globalDirectory, "test.tmp")))
            {
                HandleConfigurationProblem("The global directory is writeable", ignoreConfigIssues);
            }

            _client.MessageCreated += OnMessageCreated;
            await _client.ConnectAsync();
        }

        private void HandleConfigurationProblem(string issue, bool ignoreConfigIssues)
        {
            var errorString = $"PowerCord has detected a problem with your configuration. {issue}. This may allow users unauthorised access to your computer.";
            if (ignoreConfigIssues)
            {
                errorString += "\r\nYou have chosen to ignore configuration errors.";
                _logger.LogWarning(errorString);
            }
            else
            {
                errorString += "\r\nFor your safety, PowerCord will now terminate.\r\n";
                _logger.LogError(errorString);

                throw new InvalidOperationException(issue);
            }
        }

        private async Task OnMessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            if (e.Author.IsCurrent || e.Author.IsBot)
                return;

            if (e.Channel.IsPrivate)
            {
                _logger.LogWarning("{User} tried to run command in DMs!", e.Author);
                await e.Channel.SendMessageAsync("Sorry! PowerCord only works in servers!");
                return;
            }

            var run = false;
            var text = e.Message.Content.Trim();
            if (text.StartsWith('`') && text.EndsWith('`'))
                text = text.Trim('`');

            if (text.StartsWith(_prefix))
            {
                text = text.Substring(_prefix.Length);
                run = true;
            }

            if (text.ToLowerInvariant().StartsWith("ps\n"))
            {
                text = text.Substring(3);
                run = true;
            }

            if (!run) return;

            await ExecuteAsync(e, text);
        }

        private async Task ExecuteAsync(MessageCreateEventArgs e, string text)
        {
            DiscordRunspace runspace;

            if (!_runspaces.TryGetValue(e.Guild.Id, out runspace))
            {
                try
                {
                    runspace = _runspaces[e.Guild.Id] = new DiscordRunspace(_client, e.Guild, _loggerFactory.CreateLogger<DiscordRunspace>(), _configuration, _hostEnvironment);
                    _logger.LogInformation("Created Runspace for {Guild}", e.Guild);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create Runspace for {Guild}", e.Guild);
                    return;
                }
            }

            await runspace.ExecuteStringAsync(e.Message, text);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _client.DisconnectAsync();
        }

        private bool TestPath(string path)
        {
            try
            {
                using (File.Create(path)) { }
                try { File.Delete(path); } catch { }

                return true;
            }
            catch (UnauthorizedAccessException) { return false; }
            catch { return true; }
        }
    }
}

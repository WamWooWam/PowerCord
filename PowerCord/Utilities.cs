using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerShell.Commands;
using Microsoft.PowerShell.Commands.Internal.Format;
using Microsoft.PowerShell.Commands.Utility;
using PowerCord.Commands;
using PowerCord.Helpers;

namespace PowerCord
{
    internal static class Utilities
    {
        internal const string StartupScript = @"
            set-location user:
        ";

        internal static Type[] AllowedExternalCommandTypes
            = new[] {
                // create
                typeof(NewEventCommand), // tentative
                typeof(NewAliasCommand), // tentative
                typeof(NewGuidCommand),
                typeof(NewTimeSpanCommand),
                typeof(NewVariableCommand),

                // remove
                typeof(RemoveEventCommand),
                typeof(RemoveAliasCommand), // tentative
                typeof(RemoveVariableCommand),

                // select
                typeof(SelectObjectCommand),
                typeof(SelectStringCommand),
                typeof(SelectXmlCommand),

                // get
                typeof(GetDateCommand),
                typeof(GetRandomCommand),
                typeof(GetCultureCommand),
                typeof(GetVariableCommand),
                typeof(GetUniqueCommand),
                typeof(GetHelpCommand),
                typeof(GetMemberCommand),
                typeof(GetFileCommand),
                typeof(GetFileHashCommand),

                // set                
                typeof(SetAliasCommand),
                typeof(SetVariableCommand),

                // add
                typeof(AddMemberCommand),

                // convert
                typeof(TestJsonCommand),
                typeof(ConvertToJsonCommand),
                typeof(ConvertFromJsonCommand),
                typeof(ConvertToCsvCommand),
                typeof(ConvertFromCsvCommand),
                typeof(ConvertToXmlCommand),
                typeof(ConvertToHtmlCommand),
                typeof(ConvertFromStringDataCommand),
                typeof(ConvertFromMarkdownCommand),

                // export
                typeof(ExportCsvCommand),

                // format
                typeof(FormatListCommand),
                typeof(FormatTableCommand),
                typeof(FormatWideCommand),
                typeof(FormatDefaultCommand),
                typeof(FormatCustomCommand),
                typeof(FormatHex),

                // write
                typeof(WriteHostCommand),
                typeof(WriteDebugCommand),
                typeof(WriteErrorCommand),
                typeof(WriteWarningCommand),
                typeof(WriteVerboseCommand),
                typeof(WriteProgressCommand),
                typeof(WriteInformationCommand),
                typeof(WriteOutputCommand),

                // misc
                typeof(CompareObjectCommand),
                typeof(InvokeWebRequestCommand),
                typeof(InvokeRestMethodCommand),
                typeof(InvokeExpressionCommand),
                typeof(MeasureObjectCommand),
                typeof(SortObjectCommand),
                typeof(JoinStringCommand),
                typeof(OutStringCommand),
                typeof(OutNullCommand),
                typeof(ForEachObjectCommand),
                typeof(WhereObjectCommand),
                typeof(SelectStringCommand),

                // discord
                typeof(GetPingCommand),
                typeof(CreateDiscordMessageCommand),
                typeof(DeleteDiscordMessageCommand),
                typeof(GetAllDiscordMembersCommand),
                typeof(GetDiscordUserCommand),
                typeof(GetDiscordGuildCommand),
                typeof(GetDiscordMessageCommand),
                typeof(GetDiscordMessagesCommand),
                typeof(GetDiscordChannelCommand),
                typeof(GetDiscordMemberCommand),
                typeof(GetSnowflakeTimestampCommand),
                typeof(ModifyDiscordMessageCommand),
                typeof(ReloadRunspaceCommand),

                // bad
                typeof(GetCommandCommand),
                typeof(AddContentCommand),
                typeof(ClearContentCommand),
                typeof(ClearItemCommand),
                typeof(ClearItemPropertyCommand),
                typeof(CopyItemCommand),
                typeof(CopyItemPropertyCommand),
                typeof(GetContentCommand),
                typeof(GetChildItemCommand),
                typeof(GetItemCommand),
                typeof(GetItemPropertyCommand),
                typeof(GetPSDriveCommand),
                typeof(MoveItemCommand),
                typeof(MoveItemPropertyCommand),
                typeof(NewItemCommand),
                typeof(NewItemPropertyCommand),
                typeof(OutFileCommand),
                typeof(RemoveItemCommand),
                typeof(RemoveItemPropertyCommand),
                typeof(RenameItemCommand),
                typeof(RenameItemPropertyCommand),
                typeof(SetContentCommand),
                typeof(SetItemCommand),
                typeof(SetItemPropertyCommand),
                typeof(SetLocationCommand),
                typeof(TeeObjectCommand),
                typeof(TestConnectionCommand),
                typeof(SetStrictModeCommand),
                typeof(TestPathCommand),
                
                // modules (also bad)
                typeof(GetModuleCommand),
#if DEBUG
                typeof(ImportModuleCommand),
#endif
                typeof(ExportModuleMemberCommand),
            };

        internal static readonly SessionStateFunctionEntry[] BuiltInFunctions = new SessionStateFunctionEntry[]
        {
            // Functions that don't require full language mode
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("cd..", "Set-Location ..", isProductCode: true,languageMode: PSLanguageMode.ConstrainedLanguage),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("cd\\", "Set-Location \\", isProductCode: true, languageMode: PSLanguageMode.ConstrainedLanguage),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("help", InitialSessionState.GetHelpPagingFunctionText(), isProductCode: true,languageMode: PSLanguageMode.ConstrainedLanguage),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("mkdir", InitialSessionState.GetMkdirFunctionText(), isProductCode: true, languageMode: PSLanguageMode.FullLanguage)
        };

        internal static SessionStateAliasEntry[] BuiltInAliases
        {
            get
            {
                // Too many AllScope entries hurts performance because an entry is
                // created in each new scope, so we limit the use of AllScope to the
                // most commonly used commands - primarily so command lookup is faster,
                // though if we speed up command lookup significantly, then removing
                // AllScope for all of these aliases makes sense.

                const ScopedItemOptions AllScope = ScopedItemOptions.AllScope;
                const ScopedItemOptions ReadOnly_AllScope = ScopedItemOptions.ReadOnly | ScopedItemOptions.AllScope;
                const ScopedItemOptions ReadOnly = ScopedItemOptions.ReadOnly;

                return new SessionStateAliasEntry[] {
                    new SessionStateAliasEntry("foreach", "ForEach-Object", string.Empty, ReadOnly_AllScope),
                    new SessionStateAliasEntry("%", "ForEach-Object", string.Empty, ReadOnly_AllScope),
                    new SessionStateAliasEntry("where", "Where-Object", string.Empty, ReadOnly_AllScope),
                    new SessionStateAliasEntry("?", "Where-Object", string.Empty, ReadOnly_AllScope),
                    new SessionStateAliasEntry("clc", "Clear-Content", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("cli", "Clear-Item", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("clp", "Clear-ItemProperty", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("clv", "Clear-Variable", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("cpi", "Copy-Item", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("cvpa", "Convert-Path", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("epal", "Export-Alias", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("epcsv", "Export-Csv", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("fl", "Format-List", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("ft", "Format-Table", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("fw", "Format-Wide", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gal", "Get-Alias", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gc", "Get-Content", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gci", "Get-ChildItem", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gcm", "Get-Command", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gdr", "Get-PSDrive", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("ghy", "Get-History", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gi", "Get-Item", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gl", "Get-Location", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gm", "Get-Member", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gmo", "Get-Module", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gp", "Get-ItemProperty", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gpv", "Get-ItemPropertyValue", string.Empty,ReadOnly),
                    new SessionStateAliasEntry("group", "Group-Object", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gu", "Get-Unique", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gv", "Get-Variable", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("iex", "Invoke-Expression", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("ihy", "Invoke-History", string.Empty, ReadOnly),
                    //new SessionStateAliasEntry("ii", "Invoke-Item", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("ipmo", "Import-Module", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("ipal", "Import-Alias", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("ipcsv", "Import-Csv", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("measure", "Measure-Object", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("mi", "Move-Item", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("mp", "Move-ItemProperty", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("nal", "New-Alias", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("ni", "New-Item", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("nv", "New-Variable", string.Empty, ReadOnly),
                    //new SessionStateAliasEntry("oh", "Out-Host", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("ri", "Remove-Item", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("rni", "Rename-Item", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("rnp", "Rename-ItemProperty", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("rp", "Remove-ItemProperty", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("rv", "Remove-Variable", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gerr", "Get-Error", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("rvpa", "Resolve-Path", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("sal", "Set-Alias", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("select", "Select-Object", string.Empty, ReadOnly_AllScope),
                    new SessionStateAliasEntry("si", "Set-Item", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("sl", "Set-Location", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("sp", "Set-ItemProperty", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("sv", "Set-Variable", string.Empty, ReadOnly),
                    // Web cmdlets aliases
                    //new SessionStateAliasEntry("irm", "Invoke-RestMethod", string.Empty, ReadOnly),
                    //new SessionStateAliasEntry("iwr", "Invoke-WebRequest", string.Empty, ReadOnly),
// Porting note: #if !UNIX is used to disable aliases for cmdlets which conflict with Linux / macOS
#if !UNIX
                    // ac is a native command on macOS
                    new SessionStateAliasEntry("ac", "Add-Content", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("clear", "Clear-Host"),
                    new SessionStateAliasEntry("compare", "Compare-Object", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("cpp", "Copy-ItemProperty", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("diff", "Compare-Object", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("sort", "Sort-Object", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("tee", "Tee-Object", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("write", "Write-Output", string.Empty, ReadOnly),

                    // These were transferred from the "transferred from the profile" section
                    new SessionStateAliasEntry("cat", "Get-Content"),
                    new SessionStateAliasEntry("cp", "Copy-Item", string.Empty, AllScope),
                    new SessionStateAliasEntry("ls", "Get-ChildItem"),
                    new SessionStateAliasEntry("man", "help"),
                    new SessionStateAliasEntry("mv", "Move-Item"),
                    new SessionStateAliasEntry("rm", "Remove-Item"),
                    new SessionStateAliasEntry("rmdir", "Remove-Item"),
#endif
                    // Bash built-ins we purposefully keep even if they override native commands
                    new SessionStateAliasEntry("cd", "Set-Location", string.Empty, AllScope),
                    new SessionStateAliasEntry("dir", "Get-ChildItem", string.Empty, AllScope),
                    new SessionStateAliasEntry("echo", "Write-Output", string.Empty, AllScope),
                    new SessionStateAliasEntry("fc", "Format-Custom", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("pwd", "Get-Location"),
                    new SessionStateAliasEntry("type", "Get-Content"),
                    // Aliases transferred from the profile
                    new SessionStateAliasEntry("h", "Get-History"),
                    new SessionStateAliasEntry("history", "Get-History"),
                    new SessionStateAliasEntry("md", "mkdir", string.Empty, AllScope),
                    //new SessionStateAliasEntry("popd", "Pop-Location", string.Empty, AllScope),
                    //new SessionStateAliasEntry("pushd", "Push-Location", string.Empty, AllScope),
                    new SessionStateAliasEntry("r", "Invoke-History"),
                    //new SessionStateAliasEntry("cls", "Clear-Host"),
                    new SessionStateAliasEntry("chdir", "Set-Location"),
                    new SessionStateAliasEntry("copy", "Copy-Item", string.Empty, AllScope),
                    new SessionStateAliasEntry("del", "Remove-Item", string.Empty, AllScope),
                    new SessionStateAliasEntry("erase", "Remove-Item"),
                    new SessionStateAliasEntry("move", "Move-Item", string.Empty, AllScope),
                    new SessionStateAliasEntry("rd", "Remove-Item"),
                    new SessionStateAliasEntry("ren", "Rename-Item"),
                    new SessionStateAliasEntry("set", "Set-Variable"),
                    new SessionStateAliasEntry("icm", "Invoke-Command"),
                    new SessionStateAliasEntry("clhy", "Clear-History", string.Empty, ReadOnly),

                    // Win8: 121662/169179 Add "sls" alias for Select-String cmdlet
                    //   - do not use AllScope - this causes errors in profiles that set this somewhat commonly used alias.
                    new SessionStateAliasEntry("sls", "Select-String"),
                    new SessionStateAliasEntry("help", "Get-Help"),
                };
            }
        }

        public static void AddTypeConverter<TType, TConverter>(this InitialSessionState initialState)
        {
            var data = new TypeData(typeof(TType));
            data.TypeConverter = typeof(TConverter);
            initialState.Types.Add(new SessionStateTypeEntry(data, false));
        }

        internal static void RegisterTypes(InitialSessionState state, IEnumerable<Type> types)
        {

            foreach (var type in types.Concat(AllowedExternalCommandTypes))
            {
                var attr = type.GetCustomAttribute<CmdletAttribute>();
                if (attr != null)
                {
                    state.Commands.Add(new SessionStateCmdletEntry($"{attr.VerbName}-{attr.NounName}", type, attr.HelpUri));

                    var aliases = type.GetCustomAttribute<AliasAttribute>();
                    if (aliases != null)
                    {
                        foreach (var alias in aliases.AliasNames)
                        {
                            state.Commands.Add(new SessionStateAliasEntry(alias, $"{attr.VerbName}-{attr.NounName}"));
                        }
                    }
                }
            }

            foreach (var func in BuiltInFunctions)
            {
                state.Commands.Add(func);
            }

            foreach (var alias in BuiltInAliases)
            {
                state.Commands.Add(alias);
            }

            //state.Commands.Add(new SessionStateAliasEntry("help", "Get-Help"));
            //state.Commands.Add(new SessionStateAliasEntry("cd", "Set-Location"));
            //state.Commands.Add(new SessionStateAliasEntry("cd..", "Set-Location .."));
            //state.Commands.Add(new SessionStateAliasEntry("cd\\", "Set-Location \\"));
            //state.Commands.Add(new SessionStateAliasEntry("mkdir", "New-Item -Type Folder"));
        }

        internal static void Remove(this PSVariableIntrinsics intrinsics, string name, bool force)
        {
            try
            {
                var sessionState = intrinsics.GetType() // i'm having a fun time
                                             .GetField("_sessionState", BindingFlags.NonPublic | BindingFlags.Instance)
                                             .GetValue(intrinsics);

                var removeVariableMethod = sessionState.GetType()
                                                       .GetMethod("RemoveVariable", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(string), typeof(bool) }, null);

                removeVariableMethod.Invoke(sessionState, new object[] { name, force });
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
            catch
            {
                throw;
            }
        }

        internal static void RunWithTimeout(this Pipeline pipeline, TimeSpan timeout, out CancellationToken cancellationToken, out Exception exception, out Collection<PSObject> output)
        {
            exception = null;
            output = null;
            cancellationToken = default;
            try
            {
                using var cts = new CancellationTokenSource(timeout);
                cancellationToken = cts.Token;

                using var token = cts.Token.Register(() => pipeline.Stop());
                output = pipeline.Invoke();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        }

        internal static async Task<string> GetOutputStringAsync(this Runspace runspace, Pipeline pipeline, PSDiscordCmdletContext ctx, Collection<PSObject> prompt, Exception ex = null)
        {
            Runspace.DefaultRunspace = runspace;
            if (pipeline.HadErrors)
            {
                prompt = new Collection<PSObject>();

                var errors = pipeline.Error.NonBlockingRead().OfType<PSObject>();
                foreach (var error in errors)
                {
                    prompt.Add(error);
                }
            }

            if (ex is ParseException pex)
                prompt.Add(PSObject.AsPSObject(pex.Message));
            else if (ex is RuntimeException rex)
                prompt.Add(PSObject.AsPSObject(rex.ErrorRecord));

            if (prompt.Count == 0)
                return null;

            using var writer = new StringWriter();
            using var pp = new PipelineProcessor();
            var textWriterLineOutput = new TextWriterLineOutput(writer, 120);
            var context = runspace.ExecutionContext;

            if (!prompt.Any(o => o?.IsHelpObject == true))
            {
                var tableProcessor = new CommandProcessor(new CmdletInfo("format-table", typeof(FormatTableCommand), null, null, context), context);
                pp.Add(tableProcessor);
            }

            using var outputProcessor = new CommandProcessor(new CmdletInfo("out-lineoutput", typeof(OutLineOutputCommand), null, null, context), context);
            outputProcessor.AddParameter(CommandParameterInternal.CreateParameterWithArgument(null, "LineOutput", null, null, textWriterLineOutput, false));
            pp.Add(outputProcessor);

            foreach (var obj in prompt)
            {
                if (obj.BaseObject is PSDiscordObject discordObject)
                {
                    await discordObject.ProcessAsync(ctx);
                    discordObject.Dispose();
                    pp.Step(null);
                }
                else
                {
                    pp.Step(obj);
                }
            }

            pp.DoComplete();

            return writer.ToString();
        }
    }
}

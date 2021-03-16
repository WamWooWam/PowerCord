using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace PowerCord
{
    public class DiscordHostRawUserInterface : PSHostRawUserInterface
    {
        public override ConsoleColor ForegroundColor { get; set; }
        public override ConsoleColor BackgroundColor { get; set; }
        public override Coordinates CursorPosition { get; set; }
        public override Coordinates WindowPosition { get; set; }
        public override int CursorSize { get; set; }
        public override Size BufferSize { get; set; }
        public override Size WindowSize { get; set; }
        public override Size MaxWindowSize { get; }
        public override Size MaxPhysicalWindowSize { get; }
        public override bool KeyAvailable { get; }
        public override string WindowTitle { get; set; }

        public override void FlushInputBuffer()
        {

        }

        public override BufferCell[,] GetBufferContents(Rectangle rectangle)
        {
            return null;
        }

        public override KeyInfo ReadKey(ReadKeyOptions options)
        {
            return default;
        }

        public override void ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill)
        {

        }

        public override void SetBufferContents(Coordinates origin, BufferCell[,] contents)
        {

        }

        public override void SetBufferContents(Rectangle rectangle, BufferCell fill)
        {

        }
    }

    public class DiscordHostUserInterface : PSHostUserInterface
    {
        public override PSHostRawUserInterface RawUI { get; } = new DiscordHostRawUserInterface();

        public override Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions)
        {
            return null;
        }

        public override int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice)
        {
            // todo: make this not cringe
            if (caption == "Do you want to run software from this untrusted publisher?")
                return 3;

            return defaultChoice;
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName)
        {
            return null;
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options)
        {
            return null;
        }

        public override string ReadLine()
        {
            return "";
        }

        public override SecureString ReadLineAsSecureString()
        {
            return new SecureString();
        }

        public override void Write(string value)
        {

        }

        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {

        }

        public override void WriteDebugLine(string message)
        {

        }

        public override void WriteErrorLine(string value)
        {

        }

        public override void WriteLine(string value)
        {

        }

        public override void WriteProgress(long sourceId, ProgressRecord record)
        {

        }

        public override void WriteVerboseLine(string message)
        {

        }

        public override void WriteWarningLine(string message)
        {

        }
    }

    public class DiscordHost : PSHost
    {
        public DiscordHost(PSObject data)
        {
            PrivateData = data;
            CurrentCulture = CultureInfo.CurrentCulture;
            CurrentUICulture = CultureInfo.CurrentUICulture;
        }

        public override CultureInfo CurrentCulture { get; }
        public override CultureInfo CurrentUICulture { get; }
        public override Guid InstanceId { get; } = Guid.NewGuid();
        public override string Name => "DiscordHost";
        public override PSHostUserInterface UI { get; } = new DiscordHostUserInterface();
        public override Version Version { get; } = PSVersionInfo.PSVersion;

        public override PSObject PrivateData { get; }

        public override void EnterNestedPrompt()
        {

        }

        public override void ExitNestedPrompt()
        {

        }

        public override void NotifyBeginApplication()
        {

        }

        public override void NotifyEndApplication()
        {

        }

        public override void SetShouldExit(int exitCode)
        {

        }
    }
}

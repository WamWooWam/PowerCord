using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using PowerCord.Attributes;

namespace PowerCord.Commands
{
    [Cmdlet("Create", "DiscordMessage")]
    [Alias("Create-Message", "Send-DiscordMessage", "Send-Message")]
    [HasPermission(Permissions.AccessChannels | Permissions.SendMessages, ChannelProperty = nameof(Channel))]
    public class CreateDiscordMessageCommand : PSDiscordCmdlet
    {
        [Parameter(ValueFromPipeline = true, Position = 0, Mandatory = true, HelpMessage = "The channel to send the message in.")]
        public DiscordChannel Channel { get; set; }

        [Parameter(Position = 1, HelpMessage = "The message content.")]
        public string Content { get; set; }

        [Parameter(Position = 2, HelpMessage = "Should the message be read by TTS?")]
        public SwitchParameter TTS { get; set; } = false;

        [Parameter(Position = 3, HelpMessage = "The embed to be sent with the message.")]
        public DiscordEmbed Embed { get; set; }

        protected override async Task ProcessDiscordRecordAsync()
        {
            if (string.IsNullOrWhiteSpace(Content) && Embed == null)
            {
                WriteError(new ErrorRecord(
                    new ArgumentException("Must provide either content, embed or both, and content may not consist only of whitespace"), 
                    "SendMessageArguments",
                    ErrorCategory.InvalidArgument,
                    Content)
                );
            }

            try
            {
                var builder = new DiscordMessageBuilder()
                    .WithContent(Content)
                    .HasTTS(TTS)
                    .WithEmbed(Embed)
                    .WithAllowedMentions(Mentions.None);

                WriteObject(await Channel.SendMessageAsync(builder));
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "SendMessageFailure", ErrorCategory.NotSpecified, null));
            }
        }
    }
}

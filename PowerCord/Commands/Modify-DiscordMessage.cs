using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using PowerCord.Attributes;

namespace PowerCord.Commands
{
    [Cmdlet("Modify", "DiscordMessage")]
    public class ModifyDiscordMessageCommand : PSDiscordCmdlet
    {
        [Parameter(ValueFromPipeline = true, Position = 0, Mandatory = true)]
        public DiscordMessage Message { get; set; }

        [ValidateNotNullOrEmpty]
        [Parameter(Position = 1, HelpMessage = "The message content.")]
        public string NewContent { get; set; }

        protected override async Task ProcessDiscordRecordAsync()
        {
            try
            {
                WriteObject(await Message.ModifyAsync(NewContent));
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "ModifyMessageFailure", ErrorCategory.NotSpecified, null));
            }
        }
    }
}

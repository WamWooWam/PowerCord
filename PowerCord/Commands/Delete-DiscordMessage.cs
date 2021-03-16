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
    [Cmdlet("Delete", "DiscordMessage")]
    [HasPermission(Permissions.ManageMessages)]
    [Alias("Delete-Message", "Remove-Message", "Remove-DiscordMessage")]
    public class DeleteDiscordMessageCommand : PSDiscordCmdlet
    {
        [Parameter(ValueFromPipeline = true, Position = 0, Mandatory = true)]
        public DiscordMessage Message { get; set; }

        protected override async Task ProcessDiscordRecordAsync()
        {
            try
            {
                await Message.DeleteAsync();
                WriteObject(null);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "DeleteMessageFailure", ErrorCategory.NotSpecified, null));
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using PowerCord.Attributes;

namespace PowerCord.Commands
{
    [Alias("Get-Message")]
    [Cmdlet("Get", "DiscordMessage")]
    [HasPermission(Permissions.AccessChannels | Permissions.ReadMessageHistory, ChannelProperty = nameof(Channel))]
    public class GetDiscordMessageCommand : PSDiscordCmdlet
    {
        [Parameter(ValueFromPipeline = true, Position = 0, Mandatory = true)]
        public DiscordChannel Channel { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        public ulong[] Id { get; set; }

        protected override async Task ProcessDiscordRecordAsync()
        {
            foreach (var id in Id)
            {
                try
                {
                    WriteObject(await Channel.GetMessageAsync(id));
                }
                catch (NotFoundException)
                {
                    WriteObject(null);
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(ex, "GetMessageFailure", ErrorCategory.NotSpecified, null));
                }
            }
        }
    }
}

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
    [Alias("Get-Messages")]
    [Cmdlet("Get", "DiscordMessages")]
    [HasPermission(Permissions.AccessChannels | Permissions.ReadMessageHistory, ChannelProperty = nameof(Channel))]
    public class GetDiscordMessagesCommand : PSDiscordCmdlet
    {
        [Parameter(ValueFromPipeline = true, Position = 0, Mandatory = true)]
        public DiscordChannel Channel { get; set; }

        [Parameter(Position = 1)]
        public int Count { get; set; } = 100;

        [Parameter]
        public ulong? Before { get; set; } = null;

        [Parameter]
        public ulong? Around { get; set; } = null;

        [Parameter]
        public ulong? After { get; set; } = null;

        protected override async Task ProcessDiscordRecordAsync()
        {
            try
            {
                WriteObject(await Channel.GetMessagesAsync(Count));
            }
            catch (DSharpPlus.Exceptions.NotFoundException)
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

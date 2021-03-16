using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;

namespace PowerCord.Commands
{
    [Alias("Get-Channel")]
    [Cmdlet("Get", "DiscordChannel")]
    public class GetDiscordChannelCommand : PSDiscordCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public ulong[] Id{ get; set; }

        protected override async Task ProcessDiscordRecordAsync()
        {
            foreach (var id in Id)
            {
                try
                {
                    WriteObject(await Discord.Client.GetChannelAsync(id));
                }
                catch (DSharpPlus.Exceptions.NotFoundException)
                {
                    WriteObject(null);
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(ex, "GetChannelFailure", ErrorCategory.NotSpecified, null));
                }
            }
        }
    }
}

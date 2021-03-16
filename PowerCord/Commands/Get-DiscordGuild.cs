using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Exceptions;

namespace PowerCord.Commands
{
    [Cmdlet("Get", "DiscordGuild")]
    [Alias("Get-Guild", "Get-Server", "Get-DiscordServer")]
    public class GetDiscordGuildCommand : PSDiscordCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public ulong[] Id { get; set; }

        protected override async Task ProcessDiscordRecordAsync()
        {
            foreach (var id in Id)
            {
                try
                {
                    WriteObject(await Discord.Client.GetGuildAsync(id));
                }
                catch (NotFoundException)
                {
                    WriteObject(null);
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(ex, "GetGuildFailure", ErrorCategory.NotSpecified, null));
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

namespace PowerCord.Commands
{
    //[Alias("Get-Member")]
    [Cmdlet("Get", "DiscordMember")]
    public class GetDiscordMemberCommand : PSDiscordCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public ulong[] Id { get; set; }

        [Parameter(ValueFromPipeline = true, Position = 1)]
        public DiscordGuild Guild { get; set; }

        protected override async Task ProcessDiscordRecordAsync()
        {
            Guild ??= Discord.Guild;

            foreach (var id in Id)
            {
                try
                {
                    WriteObject(await Guild.GetMemberAsync(id));
                }
                catch (NotFoundException)
                {
                    WriteObject(null);
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(ex, "GetMemberFailure", ErrorCategory.NotSpecified, null));
                }
            }
        }
    }
}

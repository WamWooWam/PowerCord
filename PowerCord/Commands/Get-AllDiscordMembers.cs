using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace PowerCord.Commands
{
    [Alias("Get-AllMember")]
    [Cmdlet("Get", "AllDiscordMembers")]
    public class GetAllDiscordMembersCommand : PSDiscordCmdlet
    {
        [Parameter(ValueFromPipeline = true, Position = 0)]
        public DiscordGuild Guild { get; set; }

        protected override async Task ProcessDiscordRecordAsync()
        {
            Guild ??= Discord.Guild;

            foreach (var user in await Guild.GetAllMembersAsync())
            {
                WriteObject(user);
            }
        }
    }
}

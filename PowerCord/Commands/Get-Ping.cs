using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace PowerCord.Commands
{
    [Alias("ping")]
    [Cmdlet("Get", "Ping")]
    public class GetPingCommand : PSDiscordCmdlet
    {
        protected override Task ProcessDiscordRecordAsync()
        {
            WriteObject(TimeSpan.FromMilliseconds(Discord.Client.Ping));
            return Task.CompletedTask;
        }
    }
}

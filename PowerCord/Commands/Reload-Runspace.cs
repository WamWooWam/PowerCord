using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace PowerCord.Commands
{
    [Cmdlet("Reload", "Runspace")]
    [Alias("Reset-Runspace", "Reset-DiscordRunspace")]
    public class ReloadRunspaceCommand : PSDiscordCmdlet
    {
        protected override Task ProcessDiscordRecordAsync()
        {
            Discord.Runspace.SetResetFlag();
            return Task.CompletedTask;
        }
    }
}

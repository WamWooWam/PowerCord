using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace PowerCord.Commands
{
    [Alias("Get-Snowflake")]
    [Cmdlet("Get", "SnowflakeTimestamp")]
    public class GetSnowflakeTimestampCommand : PSDiscordCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public ulong[] Id { get; set; }

        protected override Task ProcessDiscordRecordAsync()
        {
            foreach (var id in Id)
            {
                WriteObject(id.GetSnowflakeTime().UtcDateTime);
            }

            return Task.CompletedTask;
        }
    }
}

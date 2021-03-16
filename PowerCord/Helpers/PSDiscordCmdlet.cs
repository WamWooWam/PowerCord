using DSharpPlus;
using DSharpPlus.Entities;
using PowerCord.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace PowerCord
{
    public record PSDiscordCmdletContext(DiscordMessage Message, DiscordUser User, DiscordMember Member, DiscordChannel Channel, DiscordGuild Guild, DiscordClient Client, DiscordRunspace Runspace);

    public class PSDiscordCmdlet : PSAsyncCmdlet
    {
        protected PSDiscordCmdletContext Discord { get; private set; }

        protected bool Terminated { get; private set; }

        protected override async Task BeginProcessingAsync()
        {
            Discord = (PSDiscordCmdletContext)GetVariableValue("ctx");

            var attributes = this.GetType()
                .GetCustomAttributes(true)
                .OfType<IChecksAttribute>();

            foreach (var attribute in attributes)
            {
                try
                {
                    if (!await attribute.RunChecksAsync(this, Discord))
                        throw new Exception(attribute.ErrorMessage);
                }
                catch (Exception ex)
                {
                    Terminated = true;
                    ThrowTerminatingError(new ErrorRecord(ex, $"{attribute.GetType().Name}_Failed", ErrorCategory.PermissionDenied, null));
                    break;
                }
            }
        }

        protected override Task ProcessRecordAsync()
        {
            return Terminated ? Task.CompletedTask : ProcessDiscordRecordAsync();
        }

        protected virtual Task ProcessDiscordRecordAsync()
        {
            return Task.CompletedTask;
        }
    }
}

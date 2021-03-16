using DSharpPlus;
using PowerCord.Attributes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace PowerCord.Commands
{
    [Alias("Get-Messages")]
    [Cmdlet("Set", "GuildCulture")]
    [HasPermission(Permissions.ManageMessages)]
    class SetGuildCultureCommand : PSDiscordCmdlet
    {
        [Parameter(Position = 0, HelpMessage = "The culture name (i.e. en_US, fr_FR).")]
        public string CultureName { get; set; }

        protected override Task ProcessDiscordRecordAsync()
        {
            try
            {
                var newCulture = CultureInfo.CreateSpecificCulture(CultureName);                
            }
            catch (CultureNotFoundException ex)
            {
                ThrowTerminatingError(new ErrorRecord(ex, "CultureNotFound", ErrorCategory.InvalidArgument, CultureName));
            }

            return Task.CompletedTask;
        }
    }
}

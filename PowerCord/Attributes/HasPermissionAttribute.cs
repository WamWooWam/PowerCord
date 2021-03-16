using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerCord.Attributes
{
    public class HasPermissionAttribute : Attribute, IChecksAttribute
    {
        private Permissions _perms;

        public bool IgnoreDMs { get; set; } = true;

        public string ChannelProperty { get; set; } = null;

        public HasPermissionAttribute(Permissions perms)
        {
            _perms = perms;
        }

        public string ErrorMessage =>
            "You don't have permission to run this command!";

        public async Task<bool> RunChecksAsync(PSDiscordCmdlet cmdlet, PSDiscordCmdletContext ctx)
        {
            if (ctx.Guild == null)
                return IgnoreDMs;

            var usr = ctx.Member;
            if (usr == null)
                return false;

            var channel = ctx.Channel;
            if (ChannelProperty != null)
            {
                channel = (DiscordChannel)cmdlet.GetType().GetProperty(ChannelProperty).GetValue(cmdlet) ?? ctx.Channel;
            }

            var pusr = channel.PermissionsFor(usr);

            var bot = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id).ConfigureAwait(false);
            if (bot == null)
                return false;
            var pbot = channel.PermissionsFor(bot);

            var usrok = ctx.Guild.Owner == usr;
            var botok = ctx.Guild.Owner == bot;

            if (!usrok)
                usrok = (pusr & Permissions.Administrator) != 0 || (pusr & _perms) == _perms;

            if (!botok)
                botok = (pbot & Permissions.Administrator) != 0 || (pbot & _perms) == _perms;

            return usrok && botok;
        }
    }
}

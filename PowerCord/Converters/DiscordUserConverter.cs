using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using System.Linq;
using System.Text.RegularExpressions;
using DSharpPlus.Entities;
using DSharpPlus;
using System.Management.Automation.Runspaces;

namespace PowerCord.Converters
{
    class DiscordUserConverter : PSTypeConverter
    {
        private static Regex UserRegex { get; }

        static DiscordUserConverter()
        {
            UserRegex = new Regex(@"^<@\!?(\d+?)>$", RegexOptions.ECMAScript | RegexOptions.Compiled);
        }

        public override bool CanConvertFrom(object sourceValue, Type destinationType)
        {
            if (sourceValue is not string || destinationType != typeof(DiscordUser))
                return false;

            return true;
        }

        public override bool CanConvertTo(object sourceValue, Type destinationType)
        {
            return CanConvertFrom(sourceValue, destinationType);
        }

        public override object ConvertFrom(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase)
        {
            if (!(sourceValue is string value))
                return null;

            var context = (PSDiscordCmdletContext)Runspace.DefaultRunspace.SessionStateProxy.GetVariable("ctx");

            if (ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var uid))
            {
                var result = context.Client.GetUserAsync(uid).GetAwaiter().GetResult();
                return result;
            }

            var m = UserRegex.Match(value);
            if (m.Success && ulong.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out uid))
            {
                var result = context.Client.GetUserAsync(uid).GetAwaiter().GetResult();
                return result;
            }

            if (ignoreCase)
                value = value.ToLowerInvariant();

            var di = value.IndexOf('#');
            var un = di != -1 ? value.Substring(0, di) : value;
            var dv = di != -1 ? value.Substring(di + 1) : null;

            var us = context.Client.Guilds.Values
                .SelectMany(xkvp => xkvp.Members.Values)
                .Where(xm => (!ignoreCase ? xm.Username : xm.Username.ToLowerInvariant()) == un && ((dv != null && xm.Discriminator == dv) || dv == null));

            var usr = us.FirstOrDefault();
            return usr;
        }

        public override object ConvertTo(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase)
        {
            return ConvertFrom(sourceValue, destinationType, formatProvider, ignoreCase);
        }
    }
}

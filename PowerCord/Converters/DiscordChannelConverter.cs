using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using System.Linq;
using System.Text.RegularExpressions;
using DSharpPlus.Entities;
using System.Management.Automation.Runspaces;
using DSharpPlus;

namespace PowerCord.Converters
{
    class DiscordChannelConverter : PSTypeConverter
    {
        private static Regex ChannelRegex { get; }

        static DiscordChannelConverter()
        {
            ChannelRegex = new Regex(@"^<#(\d+)>$", RegexOptions.ECMAScript | RegexOptions.Compiled);
        }


        public override bool CanConvertFrom(object sourceValue, Type destinationType)
        {
            if (!(sourceValue is string) && destinationType != typeof(DiscordChannel))
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

            if (ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cid))
            {
                var result = context.Client.GetChannelAsync(cid).GetAwaiter().GetResult();
                return result;
            }

            var m = ChannelRegex.Match(value);
            if (m.Success && ulong.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out cid))
            {
                var result = context.Client.GetChannelAsync(cid).GetAwaiter().GetResult();
                return result;
            }

            if (ignoreCase)
                value = value.ToLowerInvariant();
            var chn = context.Guild.Channels.Values.FirstOrDefault(xc => (!ignoreCase ? xc.Name : xc.Name.ToLowerInvariant()) == value);
            return chn;
        }

        public override object ConvertTo(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase)
        {
            return ConvertFrom(sourceValue, destinationType, formatProvider, ignoreCase);
        }
    }
}

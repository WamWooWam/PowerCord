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
    class DiscordMessageConverter : PSTypeConverter
    {
        public override bool CanConvertFrom(object sourceValue, Type destinationType)
        {
            if (sourceValue is not string || destinationType != typeof(DiscordMessage))
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
            if (!ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var uid))
                return null;

            return context.Channel.GetMessageAsync(uid).GetAwaiter().GetResult();
        }

        public override object ConvertTo(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase)
        {
            return ConvertFrom(sourceValue, destinationType, formatProvider, ignoreCase);
        }
    }
}

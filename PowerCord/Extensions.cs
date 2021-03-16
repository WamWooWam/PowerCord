using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace PowerCord
{
    public static class Extensions
    {
        private static Encoding UTF8 = new UTF8Encoding(false);

        public static IHost Build<TStartup>(this IHostBuilder builder) where TStartup : class, IStartup
        {
            builder.ConfigureServices((c, s) =>
            {
                var startup = ActivatorUtilities.GetServiceOrCreateInstance<TStartup>(s.BuildServiceProvider());
                startup.Configure(builder);
                startup.ConfigureServices(s);
            });

            return builder.Build();
        }

        internal static async Task<DiscordMessage> SendChunkedMessageAsync(this DiscordChannel channel, string content)
        {
            if (string.IsNullOrEmpty(content))
                return null;

            DiscordMessage message = null;
            var sanatisedLength = content.Length + content.Count(c => c == '`');

            if (sanatisedLength < 1990)
            {
                var builder = new StringBuilder(sanatisedLength + 10);
                builder.Append("```\n");
                foreach (var c in content)
                {
                    if (c == '`')
                        builder.Append("\\`");
                    else
                        builder.Append(c);
                }

                builder.Append("\n```");


                message = await channel.SendMessageAsync(builder.ToString());
            }
            else
            {
                using var stream = new MemoryStream();
                using var writer = new StreamWriter(stream, UTF8);
                writer.Write(content.TrimStart('\r', '\n'));
                writer.Flush();

                stream.Seek(0, SeekOrigin.Begin);

                var builder = new DiscordMessageBuilder()
                    .WithContent("The produced output was too long, here's the output as a file instead.")
                    .WithFile("message.txt", stream);
                await channel.SendMessageAsync(builder);
            }

            return message;
        }
    }

    public interface IStartup
    {
        void ConfigureServices(IServiceCollection services);

        void Configure(IHostBuilder host);
    }
}

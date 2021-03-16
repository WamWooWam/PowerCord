using System;
using System.Threading.Tasks;
using DSharpPlus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PowerCord
{
    public class Startup : IStartup
    {
        private readonly IConfiguration _configuration;
        private readonly ILoggerFactory _loggerFactory;


        public Startup(IConfiguration config, ILoggerFactory loggerFactory)
        {
            _configuration = config;
            _loggerFactory = loggerFactory;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHostedService<BotService>();

            var discordConfig = new DiscordConfiguration() { Token = _configuration["Discord:Token"], LoggerFactory = _loggerFactory, Intents = DiscordIntents.All };
            services.AddSingleton(new DiscordClient(discordConfig));
        }

        public void Configure(IHostBuilder host)
        {

        }
    }
}

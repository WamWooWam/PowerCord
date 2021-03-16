using System;
using System.IO;
using DSharpPlus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace PowerCord
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(c => c.AddEnvironmentVariables("PCORD_").AddCommandLine(args))
                .ConfigureHostConfiguration(c => c.AddEnvironmentVariables("PCORD_").AddCommandLine(args))
                .UseWindowsService()
                .Build<Startup>();

            await host.RunAsync();
        }
    }
}

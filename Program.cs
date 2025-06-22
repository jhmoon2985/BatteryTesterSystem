using BatteryTesterSystem.Interfaces;
using BatteryTesterSystem.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BatteryTesterSystem
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                Environment.Exit(0);
            };

            await host.RunAsync();
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<IDataStorage, DataStorage>();
                    services.AddSingleton<IDisplayService, ConsoleDisplayService>();
                    services.AddSingleton<IChannelDataManager, ChannelDataManager>();
                    services.AddSingleton<ITcpCommunicationService, TcpCommunicationService>();
                    services.AddHostedService<BatteryTesterService>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                });
    }
}
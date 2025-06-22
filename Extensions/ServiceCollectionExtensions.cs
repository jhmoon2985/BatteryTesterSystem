using BatteryTesterSystem.Interfaces;
using BatteryTesterSystem.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BatteryTesterSystem.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBatteryTesterServices(this IServiceCollection services)
        {
            services.AddSingleton<IDataStorage, DataStorage>();
            services.AddSingleton<IDisplayService, ConsoleDisplayService>();
            services.AddSingleton<IChannelDataManager, ChannelDataManager>();
            services.AddSingleton<ITcpCommunicationService, TcpCommunicationService>();
            services.AddHostedService<BatteryTesterService>();

            return services;
        }
    }
}
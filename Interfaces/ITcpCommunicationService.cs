using BatteryTesterSystem.Models;
using System;
using System.Threading.Tasks;

namespace BatteryTesterSystem.Interfaces
{
    public interface ITcpCommunicationService
    {
        event EventHandler<(byte[] data, int boardNumber)> DataReceived;
        Task StartAsync();
        Task StopAsync();
        Task SendCommandAsync(CommandMessage command, int boardNumber);
        bool IsConnected(int boardNumber);
    }
}
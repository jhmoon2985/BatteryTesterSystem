using BatteryTesterSystem.Models;
using System;
using System.Threading.Tasks;

namespace BatteryTesterSystem.Interfaces
{
    public interface IChannelDataManager
    {
        event EventHandler<ChannelData> ChannelDataReceived;
        Task ProcessRawDataAsync(byte[] rawData, int boardNumber);
        ChannelData? GetLatestChannelData(int channelNumber);
        void Initialize();
    }
}
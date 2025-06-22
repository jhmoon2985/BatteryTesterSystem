using BatteryTesterSystem.Models;
using System.Collections.Generic;

namespace BatteryTesterSystem.Interfaces
{
    public interface IDisplayService
    {
        void UpdateChannelDisplay(ChannelData data);
        void UpdateMultipleChannels(IEnumerable<ChannelData> channelDataList);
        void ShowSystemStatus(string status);
        void Initialize();
        void Dispose();
    }
}
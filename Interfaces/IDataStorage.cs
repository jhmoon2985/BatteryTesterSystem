using BatteryTesterSystem.Models;
using System.Threading.Tasks;

namespace BatteryTesterSystem.Interfaces
{
    public interface IDataStorage
    {
        Task SaveChannelDataAsync(ChannelData data);
        Task<bool> InitializeStorageAsync();
        Task FlushDataAsync();
        void Dispose();
    }
}
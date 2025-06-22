using BatteryTesterSystem.Interfaces;
using BatteryTesterSystem.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BatteryTesterSystem.Services
{
    public class DataStorage : IDataStorage, IDisposable
    {
        private readonly ILogger<DataStorage> _logger;
        private readonly ConcurrentQueue<ChannelData> _dataQueue;
        private readonly Timer _flushTimer;
        private readonly string _baseDataPath;
        private readonly SemaphoreSlim _writeSemaphore;
        private bool _disposed = false;

        public DataStorage(ILogger<DataStorage> logger)
        {
            _logger = logger;
            _dataQueue = new ConcurrentQueue<ChannelData>();
            _writeSemaphore = new SemaphoreSlim(1, 1);
            
            _baseDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BatteryTesterData");
            
            _flushTimer = new Timer(FlushTimerCallback, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        public async Task<bool> InitializeStorageAsync()
        {
            try
            {
                if (!Directory.Exists(_baseDataPath))
                {
                    Directory.CreateDirectory(_baseDataPath);
                }

                // 채널별 디렉토리 생성 (1-128)
                for (int i = 1; i <= 128; i++)
                {
                    var channelPath = Path.Combine(_baseDataPath, $"Channel_{i:D3}");
                    if (!Directory.Exists(channelPath))
                    {
                        Directory.CreateDirectory(channelPath);
                    }
                }

                _logger.LogInformation("Data storage initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize data storage");
                return false;
            }
        }

        public async Task SaveChannelDataAsync(ChannelData data)
        {
            if (_disposed) return;

            _dataQueue.Enqueue(data);
        }

        private async void FlushTimerCallback(object? state)
        {
            await FlushDataAsync();
        }

        public async Task FlushDataAsync()
        {
            if (_disposed) return;

            await _writeSemaphore.WaitAsync();
            try
            {
                var batchData = new StringBuilder();
                var processedCount = 0;
                var currentDate = DateTime.Now.ToString("yyyyMMdd");
                var currentHour = DateTime.Now.Hour;

                while (_dataQueue.TryDequeue(out var data) && processedCount < 1000)
                {
                    var channelPath = Path.Combine(_baseDataPath, $"Channel_{data.ChannelNumber:D3}");
                    var fileName = $"{currentDate}_{currentHour:D2}.csv";
                    var filePath = Path.Combine(channelPath, fileName);

                    // CSV 헤더 확인 및 추가
                    if (!File.Exists(filePath))
                    {
                        await File.WriteAllTextAsync(filePath, 
                            "Timestamp,Voltage,Current,Power,Capacity,Temperature,StepNumber,CycleNumber\n");
                    }

                    var csvLine = $"{data.Timestamp:yyyy-MM-dd HH:mm:ss.fff}," +
                                  $"{data.Voltage:F6}," +
                                  $"{data.Current:F6}," +
                                  $"{data.Power:F6}," +
                                  $"{data.Capacity:F6}," +
                                  $"{data.Temperature:F2}," +
                                  $"{data.StepNumber}," +
                                  $"{data.CycleNumber}\n";

                    await File.AppendAllTextAsync(filePath, csvLine);
                    processedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error flushing data to storage");
            }
            finally
            {
                _writeSemaphore.Release();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _flushTimer?.Dispose();
                FlushDataAsync().Wait();
                _writeSemaphore?.Dispose();
                _disposed = true;
            }
        }
    }
}
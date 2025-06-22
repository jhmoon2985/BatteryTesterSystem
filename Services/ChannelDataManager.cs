using BatteryTesterSystem.Interfaces;
using BatteryTesterSystem.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace BatteryTesterSystem.Services
{
    public class ChannelDataManager : IChannelDataManager
    {
        private readonly ILogger<ChannelDataManager> _logger;
        private readonly ConcurrentDictionary<int, ChannelData> _latestChannelData;

        public event EventHandler<ChannelData>? ChannelDataReceived;

        public ChannelDataManager(ILogger<ChannelDataManager> logger)
        {
            _logger = logger;
            _latestChannelData = new ConcurrentDictionary<int, ChannelData>();
        }

        public void Initialize()
        {
            _logger.LogInformation("Channel data manager initialized");
        }

        public async Task ProcessRawDataAsync(byte[] rawData, int boardNumber)
        {
            try
            {
                // 보드당 4채널씩 처리
                for (int channelOffset = 0; channelOffset < 4; channelOffset++)
                {
                    var channelNumber = (boardNumber - 1) * 4 + channelOffset + 1;
                    var channelRawData = new byte[200];
                    
                    // 각 채널별 200바이트 데이터 추출
                    if (rawData.Length >= (channelOffset + 1) * 200)
                    {
                        Array.Copy(rawData, channelOffset * 200, channelRawData, 0, 200);
                        
                        var channelData = ParseChannelData(channelRawData, channelNumber);
                        
                        _latestChannelData.AddOrUpdate(channelNumber, channelData, (key, oldValue) => channelData);
                        
                        ChannelDataReceived?.Invoke(this, channelData);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing raw data for board {BoardNumber}", boardNumber);
            }
        }

        private ChannelData ParseChannelData(byte[] rawData, int channelNumber)
        {
            // 실제 프로토콜에 맞춰 파싱 로직 구현
            // 여기서는 예시 구현
            var data = new ChannelData
            {
                ChannelNumber = channelNumber,
                Timestamp = DateTime.Now,
                RawData = rawData
            };

            // 바이트 배열에서 실제 데이터 파싱
            if (rawData.Length >= 200)
            {
                data.Voltage = BitConverter.ToSingle(rawData, 0) / 1000000.0; // µV to V
                data.Current = BitConverter.ToSingle(rawData, 4) / 1000000.0; // µA to A
                data.Power = BitConverter.ToSingle(rawData, 8) / 1000000.0;   // µW to W
                data.Capacity = BitConverter.ToSingle(rawData, 12) / 1000.0;  // mAh to Ah
                data.Temperature = BitConverter.ToSingle(rawData, 16) / 100.0; // 0.01°C to °C
                data.StepNumber = BitConverter.ToInt32(rawData, 20);
                data.CycleNumber = BitConverter.ToInt32(rawData, 24);
            }

            return data;
        }

        public ChannelData? GetLatestChannelData(int channelNumber)
        {
            _latestChannelData.TryGetValue(channelNumber, out var data);
            return data;
        }
    }
}
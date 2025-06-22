using BatteryTesterSystem.Interfaces;
using BatteryTesterSystem.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BatteryTesterSystem.Services
{
    public class BatteryTesterService : BackgroundService
    {
        private readonly ILogger<BatteryTesterService> _logger;
        private readonly IDataStorage _dataStorage;
        private readonly IDisplayService _displayService;
        private readonly IChannelDataManager _channelDataManager;
        private readonly ITcpCommunicationService _tcpCommunicationService;

        public BatteryTesterService(
            ILogger<BatteryTesterService> logger,
            IDataStorage dataStorage,
            IDisplayService displayService,
            IChannelDataManager channelDataManager,
            ITcpCommunicationService tcpCommunicationService)
        {
            _logger = logger;
            _dataStorage = dataStorage;
            _displayService = displayService;
            _channelDataManager = channelDataManager;
            _tcpCommunicationService = tcpCommunicationService;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Battery Tester Service starting...");

            // 초기화
            await _dataStorage.InitializeStorageAsync();
            _displayService.Initialize();
            _channelDataManager.Initialize();

            // 이벤트 핸들러 등록
            _channelDataManager.ChannelDataReceived += OnChannelDataReceived;
            _tcpCommunicationService.DataReceived += OnTcpDataReceived;

            // TCP 통신 시작
            await _tcpCommunicationService.StartAsync();

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Battery Tester Service running");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    // 주기적인 시스템 상태 체크
                    await CheckSystemHealthAsync();
                    
                    // 10초마다 상태 체크
                    await Task.Delay(10000, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Battery Tester Service stopping...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Battery Tester Service execution");
            }
        }

        private async Task CheckSystemHealthAsync()
        {
            var connectedBoards = 0;
            for (int i = 1; i <= 32; i++)
            {
                if (_tcpCommunicationService.IsConnected(i))
                {
                    connectedBoards++;
                }
            }

            var statusMessage = $"연결된 보드: {connectedBoards}/32, 메모리: {GC.GetTotalMemory(false) / 1024 / 1024:F1}MB";
            _displayService.ShowSystemStatus(statusMessage);

            // 메모리 정리
            if (DateTime.Now.Minute % 5 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private async void OnChannelDataReceived(object? sender, ChannelData data)
        {
            try
            {
                // 데이터 저장
                await _dataStorage.SaveChannelDataAsync(data);
                
                // 화면 업데이트
                _displayService.UpdateChannelDisplay(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling channel data for channel {ChannelNumber}", data.ChannelNumber);
            }
        }

        private async void OnTcpDataReceived(object? sender, (byte[] data, int boardNumber) args)
        {
            try
            {
                await _channelDataManager.ProcessRawDataAsync(args.data, args.boardNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing TCP data from board {BoardNumber}", args.boardNumber);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Battery Tester Service stopping...");

            // 이벤트 핸들러 해제
            _channelDataManager.ChannelDataReceived -= OnChannelDataReceived;
            _tcpCommunicationService.DataReceived -= OnTcpDataReceived;

            // TCP 통신 중지
            await _tcpCommunicationService.StopAsync();

            // 데이터 플러시
            await _dataStorage.FlushDataAsync();

            await base.StopAsync(cancellationToken);
            _logger.LogInformation("Battery Tester Service stopped");
        }
    }
}
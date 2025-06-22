// Services/ConsoleDisplayService.cs
using BatteryTesterSystem.Interfaces;
using BatteryTesterSystem.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace BatteryTesterSystem.Services
{
    public class ConsoleDisplayService : IDisplayService
    {
        private readonly ConcurrentDictionary<int, ChannelData> _latestChannelData;
        private readonly Timer _displayTimer;
        private readonly object _consoleLock = new object();
        private readonly ILogger<ConsoleDisplayService> _logger;
        private int _displayPage = 0;
        private const int CHANNELS_PER_PAGE = 32;
        private const int MAX_PAGES = 4; // 128 / 32 = 4
        private bool _isConsoleAvailable;
        private bool _isInitialized = false;

        public ConsoleDisplayService(ILogger<ConsoleDisplayService> logger)
        {
            _logger = logger;
            _latestChannelData = new ConcurrentDictionary<int, ChannelData>();
            _isConsoleAvailable = CheckConsoleAvailability();
            
            if (_isConsoleAvailable)
            {
                _displayTimer = new Timer(RefreshDisplay, null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));
            }
        }

        private bool CheckConsoleAvailability()
        {
            try
            {
                // 콘솔 사용 가능성 체크
                var _ = Console.WindowWidth;
                var __ = Console.WindowHeight;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Console not available, falling back to log-based display");
                return false;
            }
        }

        public void Initialize()
        {
            if (!_isConsoleAvailable)
            {
                _logger.LogInformation("Console display service initialized in log mode");
                return;
            }

            try
            {
                // 안전한 콘솔 초기화
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Console.Clear();
                }
                else
                {
                    // Linux/macOS에서는 ANSI 이스케이프 시퀀스 사용
                    Console.Write("\x1B[2J\x1B[H");
                }

                Console.CursorVisible = false;
                
                // 키보드 입력 처리를 위한 별도 스레드
                var keyThread = new Thread(HandleKeyInput) 
                { 
                    IsBackground = true,
                    Name = "KeyInputHandler"
                };
                keyThread.Start();
                
                ShowInitialHeader();
                _isInitialized = true;
                
                _logger.LogInformation("Console display service initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize console display, falling back to log mode");
                _isConsoleAvailable = false;
            }
        }

        private void HandleKeyInput()
        {
            if (!_isConsoleAvailable) return;

            try
            {
                while (true)
                {
                    var key = Console.ReadKey(true);
                    switch (key.Key)
                    {
                        case ConsoleKey.LeftArrow:
                            _displayPage = Math.Max(0, _displayPage - 1);
                            ShowInitialHeader();
                            break;
                        case ConsoleKey.RightArrow:
                            _displayPage = Math.Min(MAX_PAGES - 1, _displayPage + 1);
                            ShowInitialHeader();
                            break;
                        case ConsoleKey.R:
                            // R 키로 화면 새로고침
                            ShowInitialHeader();
                            break;
                        case ConsoleKey.Escape:
                            Environment.Exit(0);
                            break;
                        case ConsoleKey.Q:
                            Environment.Exit(0);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in key input handler");
            }
        }

        private void ShowInitialHeader()
        {
            if (!_isConsoleAvailable || !_isInitialized) return;

            lock (_consoleLock)
            {
                try
                {
                    // 안전한 화면 클리어
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Console.SetCursorPosition(0, 0);
                    }
                    else
                    {
                        Console.Write("\x1B[H"); // 커서를 홈으로 이동
                    }

                    Console.WriteLine("=== 배터리 테스터 실시간 모니터링 시스템 ===".PadRight(120));
                    Console.WriteLine("← → 키로 페이지 전환 | R: 새로고침 | ESC/Q: 종료".PadRight(120));
                    Console.WriteLine($"페이지: {_displayPage + 1}/{MAX_PAGES} (채널 {_displayPage * CHANNELS_PER_PAGE + 1}-{(_displayPage + 1) * CHANNELS_PER_PAGE})".PadRight(120));
                    Console.WriteLine(new string('=', 120));
                    Console.WriteLine("CH# | 전압(V)  | 전류(A)  | 전력(W)  | 용량(Ah) | 온도(°C) | Step | Cycle | 상태     | 최종업데이트".PadRight(120));
                    Console.WriteLine(new string('-', 120));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error showing header");
                }
            }
        }

        public void UpdateChannelDisplay(ChannelData data)
        {
            _latestChannelData.AddOrUpdate(data.ChannelNumber, data, (key, oldValue) => data);
            
            // 콘솔 사용 불가시 로그로 대체
            if (!_isConsoleAvailable)
            {
                if (data.ChannelNumber % 10 == 1) // 10개 채널마다 로그 출력
                {
                    _logger.LogInformation("Channel {Channel}: V={Voltage:F3}, I={Current:F3}, P={Power:F3}, T={Temperature:F1}°C", 
                        data.ChannelNumber, data.Voltage, data.Current, data.Power, data.Temperature);
                }
            }
        }

        public void UpdateMultipleChannels(IEnumerable<ChannelData> channelDataList)
        {
            foreach (var data in channelDataList)
            {
                UpdateChannelDisplay(data);
            }
        }

        private void RefreshDisplay(object? state)
        {
            if (!_isConsoleAvailable || !_isInitialized) return;

            lock (_consoleLock)
            {
                try
                {
                    // 헤더 업데이트
                    Console.SetCursorPosition(0, 2);
                    Console.Write($"페이지: {_displayPage + 1}/{MAX_PAGES} (채널 {_displayPage * CHANNELS_PER_PAGE + 1}-{(_displayPage + 1) * CHANNELS_PER_PAGE})".PadRight(120));

                    // 현재 페이지의 채널 데이터 표시
                    var startChannel = _displayPage * CHANNELS_PER_PAGE + 1;
                    var endChannel = Math.Min((_displayPage + 1) * CHANNELS_PER_PAGE, 128);

                    for (int ch = startChannel; ch <= endChannel; ch++)
                    {
                        var row = 6 + (ch - startChannel);
                        
                        // 화면 경계 체크
                        if (row >= Console.WindowHeight - 3) break;
                        
                        Console.SetCursorPosition(0, row);

                        if (_latestChannelData.TryGetValue(ch, out var data))
                        {
                            var status = GetChannelStatus(data);
                            var line = $"{ch,3} | {data.Voltage,8:F3} | {data.Current,8:F3} | {data.Power,8:F3} | " +
                                      $"{data.Capacity,8:F3} | {data.Temperature,8:F1} | {data.StepNumber,4} | " +
                                      $"{data.CycleNumber,5} | {status,8} | {data.Timestamp:HH:mm:ss.fff}";
                            
                            Console.Write(line.PadRight(Math.Min(120, Console.WindowWidth - 1)));
                        }
                        else
                        {
                            var line = $"{ch,3} | {"--",8} | {"--",8} | {"--",8} | {"--",8} | {"--",8} | " +
                                      $"{"--",4} | {"--",5} | {"대기중",8} | {"--",12}";
                            Console.Write(line.PadRight(Math.Min(120, Console.WindowWidth - 1)));
                        }
                    }

                    // 시스템 상태 표시
                    var statusRow = Math.Min(6 + CHANNELS_PER_PAGE + 2, Console.WindowHeight - 2);
                    Console.SetCursorPosition(0, statusRow);
                    Console.Write($"시스템 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss} | " +
                                 $"활성 채널: {_latestChannelData.Count}/128 | " +
                                 $"메모리 사용량: {GC.GetTotalMemory(false) / 1024 / 1024:F1}MB".PadRight(Console.WindowWidth - 1));
                }
                catch (ArgumentOutOfRangeException)
                {
                    // 콘솔 크기 변경으로 인한 오류는 무시
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error refreshing display");
                }
            }
        }

        private string GetChannelStatus(ChannelData data)
        {
            var timeDiff = DateTime.Now - data.Timestamp;
            if (timeDiff.TotalSeconds > 5)
                return "연결끊김";
            else if (Math.Abs(data.Current) > 0.001)
                return data.Current > 0 ? "충전중" : "방전중";
            else
                return "대기중";
        }

        public void ShowSystemStatus(string status)
        {
            if (!_isConsoleAvailable)
            {
                _logger.LogInformation("System Status: {Status}", status);
                return;
            }

            lock (_consoleLock)
            {
                try
                {
                    var statusRow = Console.WindowHeight - 1;
                    Console.SetCursorPosition(0, statusRow);
                    Console.Write($"상태: {status}".PadRight(Console.WindowWidth - 1));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error showing system status");
                }
            }
        }

        public void Dispose()
        {
            _displayTimer?.Dispose();
        }
    }
}
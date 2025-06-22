using BatteryTesterSystem.Interfaces;
using BatteryTesterSystem.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace BatteryTesterSystem.Services
{
    public class TcpCommunicationService : ITcpCommunicationService
    {
        private readonly ILogger<TcpCommunicationService> _logger;
        private readonly ConcurrentDictionary<int, TcpClient> _boardClients;
        private readonly ConcurrentDictionary<int, NetworkStream> _boardStreams;
        private readonly CancellationTokenSource _cancellationTokenSource;
        
        public event EventHandler<(byte[] data, int boardNumber)>? DataReceived;

        private const string BASE_IP = "192.168.1."; // 보드 IP 베이스
        private const int BASE_PORT = 8000;

        public TcpCommunicationService(ILogger<TcpCommunicationService> logger)
        {
            _logger = logger;
            _boardClients = new ConcurrentDictionary<int, TcpClient>();
            _boardStreams = new ConcurrentDictionary<int, NetworkStream>();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            _logger.LogInformation("Starting TCP communication service");

            // 32개 보드에 동시 연결
            var connectionTasks = new Task[32];
            for (int boardNumber = 1; boardNumber <= 32; boardNumber++)
            {
                connectionTasks[boardNumber - 1] = ConnectToBoardAsync(boardNumber);
            }

            await Task.WhenAll(connectionTasks);
            _logger.LogInformation("TCP communication service started");
        }

        private async Task ConnectToBoardAsync(int boardNumber)
        {
            try
            {
                var ip = $"{BASE_IP}{boardNumber + 100}"; // 192.168.1.101 ~ 192.168.1.132
                var port = BASE_PORT + boardNumber;

                var client = new TcpClient();
                await client.ConnectAsync(IPAddress.Parse(ip), port);
                
                client.ReceiveBufferSize = 8192;
                client.SendBufferSize = 4096;
                
                var stream = client.GetStream();
                
                _boardClients.TryAdd(boardNumber, client);
                _boardStreams.TryAdd(boardNumber, stream);

                // 각 보드별 수신 스레드 시작
                _ = Task.Run(() => ReceiveDataFromBoard(boardNumber, stream), _cancellationTokenSource.Token);

                _logger.LogInformation("Connected to board {BoardNumber} at {IP}:{Port}", boardNumber, ip, port);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to board {BoardNumber}", boardNumber);
            }
        }

        private async Task ReceiveDataFromBoard(int boardNumber, NetworkStream stream)
        {
            var buffer = new byte[8192]; // 4채널 * 200바이트 * 2 (버퍼링)
            
            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, 800, _cancellationTokenSource.Token); // 4채널 * 200바이트
                    
                    if (bytesRead == 800) // 정확한 데이터 크기 확인
                    {
                        var data = new byte[bytesRead];
                        Array.Copy(buffer, 0, data, 0, bytesRead);
                        
                        DataReceived?.Invoke(this, (data, boardNumber));
                    }
                    else if (bytesRead == 0)
                    {
                        _logger.LogWarning("Board {BoardNumber} disconnected", boardNumber);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving data from board {BoardNumber}", boardNumber);
            }
        }

        public async Task SendCommandAsync(CommandMessage command, int boardNumber)
        {
            try
            {
                if (_boardStreams.TryGetValue(boardNumber, out var stream))
                {
                    var commandData = SerializeCommand(command);
                    await stream.WriteAsync(commandData, 0, commandData.Length);
                    await stream.FlushAsync();
                    
                    _logger.LogDebug("Sent command {CommandType} to board {BoardNumber}", command.Type, boardNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send command to board {BoardNumber}", boardNumber);
            }
        }

        private byte[] SerializeCommand(CommandMessage command)
        {
            // 실제 프로토콜에 맞춰 명령어 직렬화
            var data = new byte[16];
            data[0] = (byte)command.Type;
            data[1] = (byte)command.ChannelNumber;
            
            if (command.Data != null && command.Data.Length > 0)
            {
                Array.Copy(command.Data, 0, data, 2, Math.Min(command.Data.Length, 14));
            }
            
            return data;
        }

        public bool IsConnected(int boardNumber)
        {
            return _boardClients.TryGetValue(boardNumber, out var client) && client.Connected;
        }

        public async Task StopAsync()
        {
            _logger.LogInformation("Stopping TCP communication service");
            
            _cancellationTokenSource.Cancel();
            
            foreach (var kvp in _boardStreams)
            {
                kvp.Value?.Close();
                kvp.Value?.Dispose();
            }
            
            foreach (var kvp in _boardClients)
            {
                kvp.Value?.Close();
                kvp.Value?.Dispose();
            }
            
            _boardClients.Clear();
            _boardStreams.Clear();
            
            _logger.LogInformation("TCP communication service stopped");
        }
    }
}
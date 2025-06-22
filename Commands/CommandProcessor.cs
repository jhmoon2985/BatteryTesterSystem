using BatteryTesterSystem.Interfaces;
using BatteryTesterSystem.Models;
using BatteryTesterSystem.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BatteryTesterSystem.Commands
{
    public class CommandProcessor
    {
        private readonly ITcpCommunicationService _tcpService;
        private readonly ILogger<CommandProcessor> _logger;

        public CommandProcessor(ITcpCommunicationService tcpService, ILogger<CommandProcessor> logger)
        {
            _tcpService = tcpService;
            _logger = logger;
        }

        public async Task StartAllChannelsAsync()
        {
            _logger.LogInformation("Starting all channels...");
            
            for (int board = 1; board <= 32; board++)
            {
                for (int channel = 1; channel <= 4; channel++)
                {
                    var channelNumber = (board - 1) * 4 + channel;
                    var command = ProtocolHelper.CreateStartCommand(channelNumber);
                    await _tcpService.SendCommandAsync(command, board);
                }
            }
            
            _logger.LogInformation("All channels start command sent");
        }

        public async Task StopAllChannelsAsync()
        {
            _logger.LogInformation("Stopping all channels...");
            
            for (int board = 1; board <= 32; board++)
            {
                for (int channel = 1; channel <= 4; channel++)
                {
                    var channelNumber = (board - 1) * 4 + channel;
                    var command = ProtocolHelper.CreateStopCommand(channelNumber);
                    await _tcpService.SendCommandAsync(command, board);
                }
            }
            
            _logger.LogInformation("All channels stop command sent");
        }

        public async Task StartChannelAsync(int channelNumber)
        {
            if (channelNumber < 1 || channelNumber > 128)
            {
                throw new ArgumentException("Channel number must be between 1 and 128");
            }

            var boardNumber = ((channelNumber - 1) / 4) + 1;
            var command = ProtocolHelper.CreateStartCommand(channelNumber);
            await _tcpService.SendCommandAsync(command, boardNumber);
            
            _logger.LogInformation("Channel {ChannelNumber} start command sent", channelNumber);
        }

        public async Task StopChannelAsync(int channelNumber)
        {
            if (channelNumber < 1 || channelNumber > 128)
            {
                throw new ArgumentException("Channel number must be between 1 and 128");
            }

            var boardNumber = ((channelNumber - 1) / 4) + 1;
            var command = ProtocolHelper.CreateStopCommand(channelNumber);
            await _tcpService.SendCommandAsync(command, boardNumber);
            
            _logger.LogInformation("Channel {ChannelNumber} stop command sent", channelNumber);
        }

        public async Task SendStepDataRequestAsync(int channelNumber, uint stepDataIndex)
        {
            if (channelNumber < 1 || channelNumber > 128)
            {
                throw new ArgumentException("Channel number must be between 1 and 128");
            }

            var boardNumber = ((channelNumber - 1) / 4) + 1;
            var request = ProtocolHelper.CreateStepDataRequest((ushort)channelNumber, stepDataIndex);
            var requestBytes = ProtocolHelper.StructToByteArray(request);
            
            var command = new CommandMessage
            {
                Type = CommandType.GetStatus,
                ChannelNumber = channelNumber,
                Data = requestBytes
            };
            
            await _tcpService.SendCommandAsync(command, boardNumber);
            _logger.LogDebug("Step data request sent for channel {ChannelNumber}", channelNumber);
        }
    }
}
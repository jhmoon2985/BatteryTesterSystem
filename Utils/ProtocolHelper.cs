using BatteryTesterSystem.Models;
using System;
using System.Runtime.InteropServices;

namespace BatteryTesterSystem.Utils
{
    public static class ProtocolHelper
    {
        public static byte[] StructToByteArray<T>(T structure) where T : struct
        {
            var size = Marshal.SizeOf<T>();
            var byteArray = new byte[size];
            var ptr = Marshal.AllocHGlobal(size);
            
            try
            {
                Marshal.StructureToPtr(structure, ptr, true);
                Marshal.Copy(ptr, byteArray, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            
            return byteArray;
        }

        public static T ByteArrayToStruct<T>(byte[] byteArray) where T : struct
        {
            var size = Marshal.SizeOf<T>();
            if (byteArray.Length < size)
                throw new ArgumentException($"Byte array too small. Expected {size}, got {byteArray.Length}");

            var ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(byteArray, 0, ptr, size);
                return Marshal.PtrToStructure<T>(ptr);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        public static CommandMessage CreateStartCommand(int channelNumber)
        {
            return new CommandMessage
            {
                Type = CommandType.Start,
                ChannelNumber = channelNumber,
                Data = BitConverter.GetBytes(DateTime.Now.Ticks)
            };
        }

        public static CommandMessage CreateStopCommand(int channelNumber)
        {
            return new CommandMessage
            {
                Type = CommandType.Stop,
                ChannelNumber = channelNumber,
                Data = BitConverter.GetBytes(DateTime.Now.Ticks)
            };
        }

        public static StepDataReq CreateStepDataRequest(ushort channel, uint stepDataIndex)
        {
            var request = new StepDataReq
            {
                CH = channel,
                ID = 0x0202,
                StepDataIndex = stepDataIndex
            };
            
            request.Convert();
            return request;
        }
    }
}
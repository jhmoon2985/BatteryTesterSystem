using System;
using System.Runtime.InteropServices;

namespace BatteryTesterSystem.Models
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct StepDataReq
    {
        [MarshalAs(UnmanagedType.U2)]
        public ushort CH;
        [MarshalAs(UnmanagedType.U2)]
        public ushort ID;
        [MarshalAs(UnmanagedType.U4)]
        public uint StepDataIndex;

        public void Convert()
        {
            CH = System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(CH);
            ID = System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(ID);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct StepDataAck
    {
        [MarshalAs(UnmanagedType.U2)]
        public ushort CH;
        [MarshalAs(UnmanagedType.U2)]
        public ushort ID;
        [MarshalAs(UnmanagedType.U4)]
        public uint StepDataIndex;
        [MarshalAs(UnmanagedType.U2)]
        public ushort TestType;
        [MarshalAs(UnmanagedType.U2)]
        public ushort TestMode;
        [MarshalAs(UnmanagedType.U2)]
        public ushort CycleNo;
        [MarshalAs(UnmanagedType.U4)]
        public uint StepNo;
        [MarshalAs(UnmanagedType.I4)]
        public int TargetVoltage;
        [MarshalAs(UnmanagedType.I4)]
        public int TargetCurrent;
        [MarshalAs(UnmanagedType.U2)]
        public ushort TargetChamberMode;
        [MarshalAs(UnmanagedType.I2)]
        public short TargetChamberTemp;
        [MarshalAs(UnmanagedType.I4)]
        public int TargetPower;
        [MarshalAs(UnmanagedType.I4)]
        public int TargetResistance;
        [MarshalAs(UnmanagedType.U8)]
        public ulong EndTime;
        [MarshalAs(UnmanagedType.U4)]
        public uint TimeEndIndex;

        public void Convert()
        {
            CH = System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(CH);
            ID = System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(ID);
        }
    }

    public class ChannelData
    {
        public int ChannelNumber { get; set; }
        public DateTime Timestamp { get; set; }
        public double Voltage { get; set; }
        public double Current { get; set; }
        public double Power { get; set; }
        public double Capacity { get; set; }
        public double Temperature { get; set; }
        public int StepNumber { get; set; }
        public int CycleNumber { get; set; }
        public byte[] RawData { get; set; } = new byte[200];
    }

    public class CommandMessage
    {
        public CommandType Type { get; set; }
        public int ChannelNumber { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }

    public enum CommandType
    {
        Start,
        Stop,
        Pause,
        Resume,
        Reset,
        GetStatus
    }
}
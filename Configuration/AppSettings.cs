namespace BatteryTesterSystem.Configuration
{
    public class AppSettings
    {
        public TcpSettings TcpSettings { get; set; } = new TcpSettings();
        public DataStorageSettings DataStorageSettings { get; set; } = new DataStorageSettings();
        public DisplaySettings DisplaySettings { get; set; } = new DisplaySettings();
    }

    public class TcpSettings
    {
        public string BaseIpAddress { get; set; } = "192.168.1.";
        public int BasePort { get; set; } = 8000;
        public int ReceiveBufferSize { get; set; } = 8192;
        public int SendBufferSize { get; set; } = 4096;
        public int ConnectionTimeoutMs { get; set; } = 5000;
        public int MaxReconnectAttempts { get; set; } = 3;
    }

    public class DataStorageSettings
    {
        public string DataPath { get; set; } = "BatteryTesterData";
        public int FlushIntervalMs { get; set; } = 1000;
        public int MaxBatchSize { get; set; } = 1000;
        public bool CompressData { get; set; } = false;
        public int RetentionDays { get; set; } = 30;
    }

    public class DisplaySettings
    {
        public int RefreshIntervalMs { get; set; } = 100;
        public int ChannelsPerPage { get; set; } = 32;
        public bool ShowRawData { get; set; } = false;
        public string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";
    }
}

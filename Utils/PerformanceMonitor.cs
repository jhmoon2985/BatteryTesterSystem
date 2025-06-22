using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace BatteryTesterSystem.Utils
{
    public class PerformanceMonitor
    {
        private readonly ILogger<PerformanceMonitor> _logger;
        private readonly Timer _monitorTimer;
        private long _totalPacketsReceived;
        private long _totalPacketsProcessed;
        private long _lastPacketsReceived;
        private long _lastPacketsProcessed;
        private DateTime _lastMeasurement;

        public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
        {
            _logger = logger;
            _lastMeasurement = DateTime.Now;
            _monitorTimer = new Timer(LogPerformanceMetrics, null, 
                TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
        }

        public void IncrementPacketsReceived() => Interlocked.Increment(ref _totalPacketsReceived);
        public void IncrementPacketsProcessed() => Interlocked.Increment(ref _totalPacketsProcessed);

        private void LogPerformanceMetrics(object? state)
        {
            var now = DateTime.Now;
            var elapsed = now - _lastMeasurement;
            
            var currentReceived = Interlocked.Read(ref _totalPacketsReceived);
            var currentProcessed = Interlocked.Read(ref _totalPacketsProcessed);
            
            var receivedRate = (currentReceived - _lastPacketsReceived) / elapsed.TotalSeconds;
            var processedRate = (currentProcessed - _lastPacketsProcessed) / elapsed.TotalSeconds;
            
            var process = Process.GetCurrentProcess();
            var cpuUsage = GetCpuUsage();
            var memoryUsage = process.WorkingSet64 / 1024 / 1024;
            
            _logger.LogInformation(
                "Performance: Packets/sec - Received: {ReceivedRate:F1}, Processed: {ProcessedRate:F1}, " +
                "CPU: {CpuUsage:F1}%, Memory: {MemoryUsage}MB, " +
                "Total - Received: {TotalReceived}, Processed: {TotalProcessed}",
                receivedRate, processedRate, cpuUsage, memoryUsage, 
                currentReceived, currentProcessed);
            
            _lastPacketsReceived = currentReceived;
            _lastPacketsProcessed = currentProcessed;
            _lastMeasurement = now;
        }

        private double GetCpuUsage()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                return process.TotalProcessorTime.TotalMilliseconds / Environment.ProcessorCount / 
                       DateTime.Now.Subtract(process.StartTime).TotalMilliseconds * 100;
            }
            catch
            {
                return 0;
            }
        }

        public void Dispose()
        {
            _monitorTimer?.Dispose();
        }
    }
}
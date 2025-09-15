using System;
using System.Threading;
using System.Threading.Tasks;

namespace L2Market.Domain.Services
{
    /// <summary>
    /// Event args for process found event
    /// </summary>
    public class ProcessFoundEventArgs : EventArgs
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public string WindowTitle { get; set; } = string.Empty;
        public string WindowClassName { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Service for monitoring multiple game processes
    /// </summary>
    public interface IMultiProcessMonitor
    {
        /// <summary>
        /// Event fired when a new process is found
        /// </summary>
        event EventHandler<ProcessFoundEventArgs> ProcessFound;
        
        /// <summary>
        /// Start monitoring for processes
        /// </summary>
        Task StartMonitoringAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Stop monitoring
        /// </summary>
        Task StopMonitoringAsync();
        
        /// <summary>
        /// Check if monitoring is active
        /// </summary>
        bool IsMonitoring { get; }
        
        /// <summary>
        /// Process name to monitor
        /// </summary>
        string ProcessName { get; set; }
        
        /// <summary>
        /// Monitoring interval
        /// </summary>
        TimeSpan MonitoringInterval { get; set; }
        
        /// <summary>
        /// Maximum number of processes to monitor
        /// </summary>
        int MaxProcesses { get; set; }
    }
}

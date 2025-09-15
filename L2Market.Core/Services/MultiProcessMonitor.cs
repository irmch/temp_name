using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using L2Market.Domain.Services;
using Microsoft.Extensions.Logging;

namespace L2Market.Core.Services
{
    /// <summary>
    /// Service for monitoring multiple game processes
    /// </summary>
    public class MultiProcessMonitor : IMultiProcessMonitor
    {
        private readonly ILogger<MultiProcessMonitor> _logger;
        private readonly HashSet<int> _monitoredProcessIds = new HashSet<int>();
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _monitoringTask;

        public MultiProcessMonitor(ILogger<MultiProcessMonitor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ProcessName = "l2.exe";
            MonitoringInterval = TimeSpan.FromSeconds(3);
            MaxProcesses = 10;
        }

        public event EventHandler<ProcessFoundEventArgs>? ProcessFound;

        public bool IsMonitoring => _monitoringTask != null && !_monitoringTask.IsCompleted;

        public string ProcessName { get; set; }

        public TimeSpan MonitoringInterval { get; set; }

        public int MaxProcesses { get; set; }

        public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
        {
            if (IsMonitoring)
            {
                _logger.LogWarning("Monitoring is already active");
                return;
            }

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _monitoringTask = Task.Run(async () => await MonitoringLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

            _logger.LogInformation("Started monitoring for process: {ProcessName}", ProcessName);
            await Task.CompletedTask;
        }

        public async Task StopMonitoringAsync()
        {
            if (!IsMonitoring)
            {
                _logger.LogWarning("Monitoring is not active");
                return;
            }

            _cancellationTokenSource?.Cancel();
            
            if (_monitoringTask != null)
            {
                try
                {
                    await _monitoringTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancelling
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during monitoring task completion");
                }
            }

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _monitoringTask = null;

            _logger.LogInformation("Stopped monitoring for process: {ProcessName}", ProcessName);
        }

        private async Task MonitoringLoop(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Monitoring loop started for {ProcessName}", ProcessName);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await CheckForNewProcesses(cancellationToken);
                    await Task.Delay(MonitoringInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in monitoring loop");
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
            }

            _logger.LogInformation("Monitoring loop ended for {ProcessName}", ProcessName);
        }

        private async Task CheckForNewProcesses(CancellationToken cancellationToken)
        {
            try
            {
                var processes = Process.GetProcessesByName(ProcessName.Replace(".exe", ""))
                    .Where(p => !p.HasExited && !_monitoredProcessIds.Contains(p.Id))
                    .Take(MaxProcesses - _monitoredProcessIds.Count)
                    .ToList();
                
                _logger.LogDebug("Checking for processes named '{ProcessName}'. Found {Count} processes", ProcessName, processes.Count);

                foreach (var process in processes)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        var windowTitle = process.MainWindowTitle;
                        var windowClassName = await GetWindowClassNameAsync(process.Id);

                        // Check if window has the required class name
                        if (string.IsNullOrEmpty(windowClassName) || !windowClassName.Contains("l2UnrealWWindowsViewportWindow"))
                        {
                            _logger.LogDebug("Process {ProcessName} (PID: {ProcessId}) window class '{WindowClass}' is not ready yet", 
                                ProcessName, process.Id, windowClassName);
                            continue;
                        }

                        _monitoredProcessIds.Add(process.Id);
                        
                        var eventArgs = new ProcessFoundEventArgs
                        {
                            ProcessId = process.Id,
                            ProcessName = ProcessName,
                            WindowTitle = windowTitle,
                            WindowClassName = windowClassName
                        };

                        _logger.LogInformation("Found new process: {ProcessName} (PID: {ProcessId}) with window class '{WindowClass}'", 
                            ProcessName, process.Id, windowClassName);

                        ProcessFound?.Invoke(this, eventArgs);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing process {ProcessId}", process.Id);
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for new processes");
            }
        }

        private async Task<string> GetWindowClassNameAsync(int processId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var process = Process.GetProcessById(processId);
                    if (process.MainWindowHandle == IntPtr.Zero)
                        return string.Empty;

                    var className = new System.Text.StringBuilder(256);
                    var result = GetClassName(process.MainWindowHandle, className, className.Capacity);
                    
                    return result > 0 ? className.ToString() : string.Empty;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error getting window class name for process {ProcessId}", processId);
                    return string.Empty;
                }
            });
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);
    }
}

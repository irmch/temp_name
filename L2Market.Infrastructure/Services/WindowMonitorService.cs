using L2Market.Domain.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace L2Market.Infrastructure.Services
{
    /// <summary>
    /// Windows API for window enumeration
    /// </summary>
    public static class WindowsAPI
    {
        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    }

    /// <summary>
    /// Implementation of window monitoring service
    /// </summary>
    public class WindowMonitorService : IWindowMonitorService
    {
        private readonly ILogger<WindowMonitorService> _logger;

        public WindowMonitorService(ILogger<WindowMonitorService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> WaitForWindowClassAsync(int processId, string windowClassName, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Waiting for window class '{WindowClass}' in process {ProcessId} with timeout {Timeout}s", 
                windowClassName, processId, timeout.TotalSeconds);

            var startTime = DateTime.UtcNow;
            var checkInterval = TimeSpan.FromMilliseconds(500); // Check every 500ms

            while (DateTime.UtcNow - startTime < timeout)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Window class monitoring cancelled");
                    return false;
                }

                try
                {
                    // Check if process still exists
                    if (!ProcessExists(processId))
                    {
                        _logger.LogWarning("Process {ProcessId} no longer exists", processId);
                        return false;
                    }

                    if (HasWindowClass(processId, windowClassName))
                    {
                        _logger.LogInformation("Window class '{WindowClass}' found in process {ProcessId}", windowClassName, processId);
                        return true;
                    }

                    // Wait before next check
                    await Task.Delay(checkInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Window class monitoring cancelled");
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error while monitoring window class: {Error}", ex.Message);
                    await Task.Delay(checkInterval, cancellationToken);
                }
            }

            _logger.LogWarning("Timeout waiting for window class '{WindowClass}' in process {ProcessId}", windowClassName, processId);
            return false;
        }

        public bool HasWindowClass(int processId, string windowClassName)
        {
            try
            {
                if (!ProcessExists(processId))
                {
                    return false;
                }

                bool found = false;
                var targetProcessId = (uint)processId;

                WindowsAPI.EnumWindows((hWnd, lParam) =>
                {
                    try
                    {
                        WindowsAPI.GetWindowThreadProcessId(hWnd, out uint windowProcessId);
                        
                        if (windowProcessId == targetProcessId)
                        {
                            var className = new System.Text.StringBuilder(256);
                            WindowsAPI.GetClassName(hWnd, className, className.Capacity);
                            
                            if (className.ToString().Equals(windowClassName, StringComparison.OrdinalIgnoreCase))
                            {
                                found = true;
                                return false; // Stop enumeration
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error enumerating window: {Error}", ex.Message);
                    }
                    
                    return true; // Continue enumeration
                }, IntPtr.Zero);

                return found;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking window class '{WindowClass}' in process {ProcessId}: {Error}", 
                    windowClassName, processId, ex.Message);
                return false;
            }
        }

        private bool ProcessExists(int processId)
        {
            try
            {
                using var process = Process.GetProcessById(processId);
                return !process.HasExited;
            }
            catch (ArgumentException)
            {
                return false; // Process doesn't exist
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error checking if process exists: {Error}", ex.Message);
                return false;
            }
        }
    }
}

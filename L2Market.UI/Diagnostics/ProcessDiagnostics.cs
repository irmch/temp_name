using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace L2Market.UI.Diagnostics
{
    /// <summary>
    /// Diagnostic tool for process access testing
    /// </summary>
    public class ProcessDiagnostics
    {
        private readonly ILogger<ProcessDiagnostics> _logger;

        public ProcessDiagnostics(ILogger<ProcessDiagnostics> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Test if we can access a process by ID
        /// </summary>
        /// <param name="processId">Process ID to test</param>
        /// <returns>Diagnostic result</returns>
        public ProcessAccessResult TestProcessAccess(int processId)
        {
            var result = new ProcessAccessResult
            {
                ProcessId = processId,
                TestTime = DateTime.Now
            };

            try
            {
                _logger.LogInformation("Testing access to process ID: {ProcessId}", processId);

                // Try to get process by ID
                var process = Process.GetProcessById(processId);
                
                if (process == null)
                {
                    result.Success = false;
                    result.Message = "Process not found";
                    return result;
                }

                result.ProcessName = process.ProcessName;
                result.Success = true;
                result.Message = $"Successfully accessed process: {process.ProcessName}";

                // Test if we can get process modules (requires elevated privileges)
                try
                {
                    var modules = process.Modules;
                    result.CanAccessModules = true;
                    result.Message += " - Can access process modules";
                }
                catch (Exception ex)
                {
                    result.CanAccessModules = false;
                    result.Message += $" - Cannot access process modules: {ex.Message}";
                }

                // Test if we can get process threads
                try
                {
                    var threads = process.Threads;
                    result.CanAccessThreads = true;
                    result.Message += " - Can access process threads";
                }
                catch (Exception ex)
                {
                    result.CanAccessThreads = false;
                    result.Message += $" - Cannot access process threads: {ex.Message}";
                }

                _logger.LogInformation("Process access test successful: {Message}", result.Message);
            }
            catch (ArgumentException)
            {
                result.Success = false;
                result.Message = $"Process with ID {processId} not found";
                _logger.LogWarning("Process not found: {ProcessId}", processId);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Cannot access process {processId}: {ex.Message}";
                result.Exception = ex;
                _logger.LogError(ex, "Error accessing process {ProcessId}", processId);
            }

            return result;
        }

        /// <summary>
        /// Find all processes with a specific name
        /// </summary>
        /// <param name="processName">Process name to search for</param>
        /// <returns>List of found processes</returns>
        public ProcessSearchResult[] FindProcessesByName(string processName)
        {
            var results = new List<ProcessSearchResult>();

            try
            {
                _logger.LogInformation("Searching for processes with name: {ProcessName}", processName);

                var processes = Process.GetProcessesByName(processName);
                
                foreach (var process in processes)
                {
                    var result = new ProcessSearchResult
                    {
                        ProcessId = process.Id,
                        ProcessName = process.ProcessName,
                        TestTime = DateTime.Now
                    };

                    try
                    {
                        // Test access to this process
                        var accessResult = TestProcessAccess(process.Id);
                        result.Success = accessResult.Success;
                        result.Message = accessResult.Message;
                        result.CanAccessModules = accessResult.CanAccessModules;
                        result.CanAccessThreads = accessResult.CanAccessThreads;
                    }
                    catch (Exception ex)
                    {
                        result.Success = false;
                        result.Message = $"Error testing access: {ex.Message}";
                    }

                    results.Add(result);
                }

                _logger.LogInformation("Found {Count} processes with name {ProcessName}", results.Count, processName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for processes with name {ProcessName}", processName);
            }

            return results.ToArray();
        }

        /// <summary>
        /// Check if current process is running as administrator
        /// </summary>
        /// <returns>True if running as administrator</returns>
        public bool IsRunningAsAdministrator()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking administrator privileges");
                return false;
            }
        }
    }

    /// <summary>
    /// Result of process access test
    /// </summary>
    public class ProcessAccessResult
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime TestTime { get; set; }
        public bool CanAccessModules { get; set; }
        public bool CanAccessThreads { get; set; }
        public Exception? Exception { get; set; }
    }

    /// <summary>
    /// Result of process search
    /// </summary>
    public class ProcessSearchResult
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime TestTime { get; set; }
        public bool CanAccessModules { get; set; }
        public bool CanAccessThreads { get; set; }
    }
}

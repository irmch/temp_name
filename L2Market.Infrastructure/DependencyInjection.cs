using L2Market.Domain;
using L2Market.Domain.Common;
using L2Market.Domain.Services;
using L2Market.Infrastructure.EventBus;
using L2Market.Infrastructure.NamedPipeServices;
using L2Market.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace L2Market.Infrastructure
{
    /// <summary>
    /// DLL injector implementation via P/Invoke
    /// </summary>
    public class DllInjector : IDllInjector
    {
        // P/Invoke declarations for functions from L2Market.Injector.dll
        [DllImport("L2Market.Injector.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern bool InjectDLL([MarshalAs(UnmanagedType.LPStr)] string dllPath, int processId);

        [DllImport("L2Market.Injector.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, EntryPoint = "FindProcessByName", SetLastError = true)]
        private static extern int FindProcessByNameNative([MarshalAs(UnmanagedType.LPWStr)] string processName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern void SetLastError(uint dwErrCode);

        public InjectionResult InjectDll(string dllPath, int processId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dllPath))
                {
                    return new InjectionResult
                    {
                        Success = false,
                        ErrorMessage = "DLL file path cannot be empty",
                        ProcessId = processId
                    };
                }

                if (processId <= 0)
                {
                    return new InjectionResult
                    {
                        Success = false,
                        ErrorMessage = "Process ID must be greater than 0",
                        ProcessId = processId
                    };
                }

                if (!System.IO.File.Exists(dllPath))
                {
                    return new InjectionResult
                    {
                        Success = false,
                        ErrorMessage = $"DLL file not found: {dllPath}",
                        ProcessId = processId
                    };
                }

                // Check if process exists and is accessible
                try
                {
                    var process = System.Diagnostics.Process.GetProcessById(processId);
                    if (process == null)
                    {
                        return new InjectionResult
                        {
                            Success = false,
                            ErrorMessage = $"Process with ID {processId} not found",
                            ProcessId = processId
                        };
                    }
                }
                catch (ArgumentException)
                {
                    return new InjectionResult
                    {
                        Success = false,
                        ErrorMessage = $"Process with ID {processId} not found",
                        ProcessId = processId
                    };
                }
                catch (Exception ex)
                {
                    return new InjectionResult
                    {
                        Success = false,
                        ErrorMessage = $"Cannot access process {processId}: {ex.Message}",
                        ProcessId = processId
                    };
                }

                // Clear LastError before call
                SetLastError(0);
                
                bool result = InjectDLL(dllPath, processId);
                
                // Get last Windows error
                int lastError = Marshal.GetLastWin32Error();
                
                string errorMessage = string.Empty;
                if (!result)
                {
                    if (lastError == 0)
                    {
                        // DLL returned false but no Windows error - check if process exists
                        try
                        {
                            var process = System.Diagnostics.Process.GetProcessById(processId);
                            if (process == null)
                            {
                                errorMessage = $"Process with ID {processId} not found or not accessible";
                            }
                            else
                            {
                                errorMessage = $"DLL injection failed - process exists but injection was unsuccessful. Process: {process.ProcessName}";
                            }
                        }
                        catch (ArgumentException)
                        {
                            errorMessage = $"Process with ID {processId} not found";
                        }
                        catch (Exception ex)
                        {
                            errorMessage = $"Cannot access process {processId}: {ex.Message}";
                        }
                    }
                    else
                    {
                        errorMessage = $"Error during DLL injection. Windows Error: {lastError} (0x{lastError:X8})";
                    }
                }

                return new InjectionResult
                {
                    Success = result,
                    ErrorMessage = errorMessage,
                    ProcessId = processId
                };
            }
            catch (DllNotFoundException)
            {
                return new InjectionResult
                {
                    Success = false,
                    ErrorMessage = "L2Market.Injector.dll not found. Make sure the DLL is in the application folder.",
                    ProcessId = processId
                };
            }
            catch (Exception ex)
            {
                return new InjectionResult
                {
                    Success = false,
                    ErrorMessage = $"Unexpected error: {ex.Message}",
                    ProcessId = processId
                };
            }
        }

        public ProcessSearchResult FindProcessByName(string processName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(processName))
                {
                    return new ProcessSearchResult
                    {
                        Found = false,
                        ErrorMessage = "Process name cannot be empty"
                    };
                }

                // Clear LastError before call
                SetLastError(0);
                
                int processId = FindProcessByNameNative(processName);
                int lastError = Marshal.GetLastWin32Error();

                if (processId > 0)
                {
                    return new ProcessSearchResult
                    {
                        Found = true,
                        ProcessId = processId,
                        ProcessName = processName,
                        ErrorMessage = string.Empty
                    };
                }
                else
                {
                    // If process not found, this is not an error - it's a normal situation
                    string errorMessage = lastError != 0 
                        ? $"Error during process search: {lastError} (0x{lastError:X8})"
                        : $"Process '{processName}' not found or not running";

                    return new ProcessSearchResult
                    {
                        Found = false,
                        ProcessId = 0,
                        ProcessName = processName,
                        ErrorMessage = errorMessage
                    };
                }
            }
            catch (DllNotFoundException)
            {
                return new ProcessSearchResult
                {
                    Found = false,
                    ErrorMessage = "L2Market.Injector.dll not found. Make sure the DLL is in the application folder."
                };
            }
            catch (Exception ex)
            {
                return new ProcessSearchResult
                {
                    Found = false,
                    ErrorMessage = $"Unexpected error during process search: {ex.Message}"
                };
            }
        }
    }

    /// <summary>
    /// Extensions for Infrastructure layer service registration
    /// </summary>
    public static class DependencyInjectionExtensions
    {
        [SupportedOSPlatform("windows")]
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            // Event Bus
            services.AddSingleton<IEventBus, InMemoryEventBus>();
            
            // Services
            services.AddScoped<IDllInjector, DllInjector>();
            services.AddScoped<IDllInjectionService, DllInjectionService>();
            services.AddScoped<INamedPipeService, NamedPipeService>();
            services.AddScoped<IWindowMonitorService, WindowMonitorService>();
            return services;
        }
    }
}

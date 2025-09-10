using Microsoft.Extensions.DependencyInjection;
using System;

namespace L2Market.Domain
{
    /// <summary>
    /// Injection operation result
    /// </summary>
    public class InjectionResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Process search result
    /// </summary>
    public class ProcessSearchResult
    {
        public bool Found { get; set; }
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Interface for DLL injector operations
    /// </summary>
    public interface IDllInjector
    {
        /// <summary>
        /// Injects DLL into specified process
        /// </summary>
        /// <param name="dllPath">Path to DLL file</param>
        /// <param name="processId">Process ID</param>
        /// <returns>Operation result</returns>
        InjectionResult InjectDll(string dllPath, int processId);

        /// <summary>
        /// Finds process by name
        /// </summary>
        /// <param name="processName">Process name</param>
        /// <returns>Search result</returns>
        ProcessSearchResult FindProcessByName(string processName);
    }

    /// <summary>
    /// Extensions for Domain layer service registration
    /// </summary>
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddDomain(this IServiceCollection services)
        {
            // Domain layer contains no services, only interfaces and models
            return services;
        }
    }
}

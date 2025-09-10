using L2Market.Domain;
using System.Threading.Tasks;

namespace L2Market.Domain.Services
{
    /// <summary>
    /// Service for DLL injection operations
    /// </summary>
    public interface IDllInjectionService
    {
        /// <summary>
        /// Injects DLL into process
        /// </summary>
        /// <param name="dllPath">Path to DLL</param>
        /// <param name="processId">Process ID</param>
        /// <returns>Operation result</returns>
        Task<InjectionResult> InjectDllAsync(string dllPath, int processId);

        /// <summary>
        /// Finds process by name
        /// </summary>
        /// <param name="processName">Process name</param>
        /// <returns>Search result</returns>
        Task<ProcessSearchResult> FindProcessAsync(string processName);
    }
}

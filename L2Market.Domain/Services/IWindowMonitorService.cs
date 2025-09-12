using System;
using System.Threading;
using System.Threading.Tasks;

namespace L2Market.Domain.Services
{
    /// <summary>
    /// Service for monitoring window classes in processes
    /// </summary>
    public interface IWindowMonitorService
    {
        /// <summary>
        /// Waits for a specific window class to appear in a process
        /// </summary>
        /// <param name="processId">Process ID to monitor</param>
        /// <param name="windowClassName">Window class name to wait for</param>
        /// <param name="timeout">Maximum time to wait</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if window class found, false if timeout</returns>
        Task<bool> WaitForWindowClassAsync(int processId, string windowClassName, TimeSpan timeout, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a specific window class exists in a process
        /// </summary>
        /// <param name="processId">Process ID to check</param>
        /// <param name="windowClassName">Window class name to look for</param>
        /// <returns>True if window class exists</returns>
        bool HasWindowClass(int processId, string windowClassName);
    }
}

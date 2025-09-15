using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using L2Market.Domain.Models;

namespace L2Market.Domain.Services
{
    /// <summary>
    /// Service for managing multiple game process connections
    /// </summary>
    public interface IConnectionManager
    {
        /// <summary>
        /// Collection of all active connections
        /// </summary>
        ObservableCollection<ConnectionInfo> Connections { get; }
        
        /// <summary>
        /// Event fired when a new connection is added
        /// </summary>
        event EventHandler<ConnectionInfo> ConnectionAdded;
        
        /// <summary>
        /// Event fired when a connection is removed
        /// </summary>
        event EventHandler<ConnectionInfo> ConnectionRemoved;
        
        /// <summary>
        /// Event fired when connection status changes
        /// </summary>
        event EventHandler<ConnectionInfo> ConnectionStatusChanged;
        
        /// <summary>
        /// Add a new connection
        /// </summary>
        Task<ConnectionInfo> AddConnectionAsync(int processId, string processName, string windowTitle = "");
        
        /// <summary>
        /// Remove a connection
        /// </summary>
        Task<bool> RemoveConnectionAsync(Guid connectionId);
        
        /// <summary>
        /// Get connection by ID
        /// </summary>
        Task<ConnectionInfo?> GetConnectionAsync(Guid connectionId);
        
        /// <summary>
        /// Get connection by process ID
        /// </summary>
        Task<ConnectionInfo?> GetConnectionByProcessIdAsync(int processId);
        
        /// <summary>
        /// Update connection status
        /// </summary>
        Task UpdateConnectionStatusAsync(Guid connectionId, bool isConnected, string status = "");
        
        /// <summary>
        /// Check if process is already connected
        /// </summary>
        bool IsProcessConnected(int processId);
        
        /// <summary>
        /// Get total number of connections
        /// </summary>
        int ConnectionCount { get; }
        
        /// <summary>
        /// Get number of connected processes
        /// </summary>
        int ConnectedCount { get; }
    }
}

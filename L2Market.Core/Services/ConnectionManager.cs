using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using L2Market.Domain.Models;
using L2Market.Domain.Services;
using Microsoft.Extensions.Logging;

namespace L2Market.Core.Services
{
    /// <summary>
    /// Service for managing multiple game process connections
    /// </summary>
    public class ConnectionManager : IConnectionManager
    {
        private readonly ILogger<ConnectionManager> _logger;
        private readonly object _lock = new object();
        private readonly Action<Action> _uiDispatcher;

        public ConnectionManager(ILogger<ConnectionManager> logger, Action<Action>? uiDispatcher = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _uiDispatcher = uiDispatcher ?? (action => action()); // Default to synchronous execution
            Connections = new ObservableCollection<ConnectionInfo>();
        }

        public ObservableCollection<ConnectionInfo> Connections { get; }

        public event EventHandler<ConnectionInfo>? ConnectionAdded;
        public event EventHandler<ConnectionInfo>? ConnectionRemoved;
        public event EventHandler<ConnectionInfo>? ConnectionStatusChanged;

        public int ConnectionCount => Connections.Count;
        
        public int ConnectedCount => Connections.Count(c => c.IsConnected);

        public async Task<ConnectionInfo> AddConnectionAsync(int processId, string processName, string windowTitle = "")
        {
            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    // Check if process is already connected
                    var existingConnection = Connections.FirstOrDefault(c => c.ProcessId == processId);
                    if (existingConnection != null)
                    {
                        _logger.LogWarning("Process {ProcessName} (PID: {ProcessId}) is already connected", processName, processId);
                        return existingConnection;
                    }

                    var connection = new ConnectionInfo
                    {
                        ProcessId = processId,
                        ProcessName = processName,
                        WindowTitle = windowTitle,
                        IsConnected = false,
                        ConnectedAt = DateTime.UtcNow,
                        ConnectionStatus = "Connecting..."
                    };

                    // Add to collection on UI thread
                    _uiDispatcher(() =>
                    {
                        Connections.Add(connection);
                    });
                    
                    _logger.LogInformation("Added connection for {ProcessName} (PID: {ProcessId})", processName, processId);
                    
                    ConnectionAdded?.Invoke(this, connection);
                    
                    return connection;
                }
            });
        }

        public async Task<bool> RemoveConnectionAsync(Guid connectionId)
        {
            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    var connection = Connections.FirstOrDefault(c => c.ConnectionId == connectionId);
                    if (connection == null)
                    {
                        _logger.LogWarning("Connection {ConnectionId} not found for removal", connectionId);
                        return false;
                    }

                    // Remove from collection on UI thread
                    _uiDispatcher(() =>
                    {
                        Connections.Remove(connection);
                    });
                    
                    _logger.LogInformation("Removed connection for {ProcessName} (PID: {ProcessId})", connection.ProcessName, connection.ProcessId);
                    
                    ConnectionRemoved?.Invoke(this, connection);
                    
                    return true;
                }
            });
        }

        public async Task<ConnectionInfo?> GetConnectionAsync(Guid connectionId)
        {
            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    return Connections.FirstOrDefault(c => c.ConnectionId == connectionId);
                }
            });
        }

        public async Task<ConnectionInfo?> GetConnectionByProcessIdAsync(int processId)
        {
            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    return Connections.FirstOrDefault(c => c.ProcessId == processId);
                }
            });
        }

        public async Task UpdateConnectionStatusAsync(Guid connectionId, bool isConnected, string status = "")
        {
            await Task.Run(() =>
            {
                lock (_lock)
                {
                    var connection = Connections.FirstOrDefault(c => c.ConnectionId == connectionId);
                    if (connection == null)
                    {
                        _logger.LogWarning("Connection {ConnectionId} not found for status update", connectionId);
                        return;
                    }

                    var wasConnected = connection.IsConnected;
                    
                    // Update connection properties on UI thread
                    _uiDispatcher(() =>
                    {
                        connection.IsConnected = isConnected;
                        
                        if (!string.IsNullOrEmpty(status))
                        {
                            connection.ConnectionStatus = status;
                        }
                        else
                        {
                            connection.ConnectionStatus = isConnected ? "Connected" : "Disconnected";
                        }

                        if (isConnected && !wasConnected)
                        {
                            connection.ConnectedAt = DateTime.UtcNow;
                        }
                    });

                    _logger.LogInformation("Updated connection status for {ProcessName} (PID: {ProcessId}): {Status}", 
                        connection.ProcessName, connection.ProcessId, connection.ConnectionStatus);
                    
                    ConnectionStatusChanged?.Invoke(this, connection);
                }
            });
        }

        public bool IsProcessConnected(int processId)
        {
            lock (_lock)
            {
                return Connections.Any(c => c.ProcessId == processId && c.IsConnected);
            }
        }
    }
}

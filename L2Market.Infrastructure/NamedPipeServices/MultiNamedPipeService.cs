using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Runtime.Versioning;
using L2Market.Core.Configuration;
using L2Market.Domain.Common;
using L2Market.Domain.Events;
using L2Market.Domain.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace L2Market.Infrastructure.NamedPipeServices
{
    /// <summary>
    /// Service for managing multiple Named Pipes (one per connection)
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class MultiNamedPipeService : INamedPipeService, IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<MultiNamedPipeService> _logger;
        private readonly ConcurrentDictionary<uint, PipeConnection> _connections = new();
        private bool _disposed = false;

        public bool IsConnected => _connections.Values.Any(c => c.IsConnected);

        public MultiNamedPipeService(
            IEventBus eventBus, 
            IConfigurationService configurationService,
            ILogger<MultiNamedPipeService> logger)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task StartServerAsync(uint processId, CancellationToken cancellationToken = default)
        {
            try
            {
                var settings = _configurationService.Settings.NamedPipe;
                _logger.LogInformation("Starting NamedPipe server for PID: {ProcessId}", processId);
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Starting server for PID: {processId}"));
                
                // Check if connection already exists
                if (_connections.ContainsKey(processId))
                {
                    _logger.LogWarning("NamedPipe server already running for PID: {ProcessId}", processId);
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Server already running for PID: {processId}"));
                    return;
                }
                
                var pipeName = $"{processId}";
                
                _logger.LogDebug("Creating pipe: {PipeName}", pipeName);
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Creating pipe: {pipeName}"));
                
                // Create connection info
                var connection = new PipeConnection(processId, pipeName, _eventBus, _configurationService, _logger);
                _connections[processId] = connection;
                
                // Start server in background
                var serverTask = Task.Run(() => connection.ServerLoopAsync(cancellationToken));
                connection.ServerTask = serverTask;
                
                _logger.LogInformation("NamedPipe server started successfully (running in background)");
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Server started successfully (running in background)"));
                
                // Give server a moment to initialize
                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start NamedPipe server");
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Error starting server: {ex.Message}"));
                throw;
            }
        }

        public void StopServer()
        {
            try
            {
                _logger.LogInformation("Stopping all NamedPipe servers");
                
                foreach (var connection in _connections.Values)
                {
                    connection.Stop();
                }
                
                _connections.Clear();
                
                _logger.LogInformation("All NamedPipe servers stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping NamedPipe servers");
            }
        }

        public async Task RestartServerAsync(uint processId, CancellationToken cancellationToken = default)
        {
            try
            {
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Restarting server for PID: {processId}"));
                StopServer();
                await Task.Delay(1000, cancellationToken); // Small delay before restart
                await StartServerAsync(processId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restarting server");
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Error restarting server: {ex.Message}"));
            }
        }

        public async Task SendCommandAsync(string hex)
        {
            try
            {
                var json = new
                {
                    command = "sendpacket",
                    data = hex
                };
                string jsonString = System.Text.Json.JsonSerializer.Serialize(json);
                
                // Конвертируем весь JSON объект в base64
                byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonString);
                string base64Command = Convert.ToBase64String(jsonBytes);
                
                // Send to all connected pipes
                var tasks = _connections.Values
                    .Where(c => c.IsConnected && c.Writer != null)
                    .Select(c => c.Writer!.WriteLineAsync(base64Command))
                    .ToArray();
                
                if (tasks.Length > 0)
                {
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Отправка команды: {hex.Length} символов"));
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Hex данные: {hex}"));
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] JSON команда: {jsonString}"));
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Base64 команда: {base64Command}"));
                    
                    await Task.WhenAll(tasks);
                    
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Команда отправлена успешно"));
                }
                else
                {
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Ошибка: Нет подключенных пайпов. Команда не отправлена."));
                }
            }
            catch (Exception ex)
            {
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Ошибка отправки команды: {ex.Message}"));
            }
        }

        /// <summary>
        /// Отправляет команду в конкретный NamedPipe по ProcessId
        /// </summary>
        public async Task SendCommandAsync(string hex, uint processId)
        {
            try
            {
                if (!_connections.TryGetValue(processId, out var connection) || !connection.IsConnected || connection.Writer == null)
                {
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Ошибка: NamedPipe для ProcessId {processId} не найден или не подключен"));
                    return;
                }

                var json = new
                {
                    command = "sendpacket",
                    data = hex
                };
                string jsonString = System.Text.Json.JsonSerializer.Serialize(json);
                
                // Конвертируем весь JSON объект в base64
                byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonString);
                string base64Command = Convert.ToBase64String(jsonBytes);
                
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Отправка команды в ProcessId {processId}: {hex.Length} символов"));
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Hex данные: {hex}"));
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] JSON команда: {jsonString}"));
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Base64 команда: {base64Command}"));
                
                await connection.Writer.WriteLineAsync(base64Command);
                
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Команда отправлена в ProcessId {processId} успешно"));
            }
            catch (Exception ex)
            {
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Ошибка отправки команды в ProcessId {processId}: {ex.Message}"));
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                StopServer();
                _disposed = true;
            }
        }

        private class PipeConnection
        {
            public uint ProcessId { get; }
            public string PipeName { get; }
            public bool IsConnected { get; private set; }
            public Task? ServerTask { get; set; }
            public StreamWriter? Writer { get; private set; }
            
            private readonly IEventBus _eventBus;
            private readonly IConfigurationService _configurationService;
            private readonly ILogger _logger;
            private int _retryCount = 0;

            public PipeConnection(uint processId, string pipeName, IEventBus eventBus, IConfigurationService configurationService, ILogger logger)
            {
                ProcessId = processId;
                PipeName = pipeName;
                _eventBus = eventBus;
                _configurationService = configurationService;
                _logger = logger;
            }

            public async Task ServerLoopAsync(CancellationToken cancellationToken)
            {
                try
                {
                    var settings = _configurationService.Settings.NamedPipe;
                    _logger.LogInformation("Starting main loop for pipe: {PipeName} with buffer size: {BufferSize}", PipeName, settings.BufferSize);
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Starting main loop for pipe: {PipeName} with buffer size: {settings.BufferSize} bytes"));
                    
                    using var server = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.InOut,
                        1,
                        PipeTransmissionMode.Message,
                        PipeOptions.Asynchronous,
                        settings.BufferSize,
                        settings.BufferSize
                    );

                    _logger.LogDebug("Waiting for client connection to pipe {PipeName} (timeout: {Timeout}s)...", PipeName, settings.ConnectionTimeout.TotalSeconds);
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Waiting for client connection to pipe {PipeName} (timeout: {settings.ConnectionTimeout.TotalSeconds}s)..."));

                    bool connected = false;
                    try
                    {
                        var connectTask = server.WaitForConnectionAsync(cancellationToken);
                        var timeoutTask = Task.Delay(settings.ConnectionTimeout, cancellationToken);
                        var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                        
                        if (completedTask == connectTask && server.IsConnected)
                        {
                            connected = true;
                            IsConnected = true;
                            _retryCount = 0;
                            _logger.LogInformation("Client connected to pipe {PipeName}", PipeName);
                            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Client connected to pipe {PipeName}."));
                            await _eventBus.PublishAsync(new ConnectionStatusChangedEvent(true, PipeName));
                        }
                        else
                        {
                            _retryCount++;
                            _logger.LogWarning("Client connection timeout ({Timeout}s). Attempt #{Attempt} of {MaxRetries}", 
                                settings.ConnectionTimeout.TotalSeconds, _retryCount, settings.MaxRetries);
                            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Client connection timeout ({settings.ConnectionTimeout.TotalSeconds}s). Attempt #{_retryCount} of {settings.MaxRetries}"));
                            
                            if (_retryCount < settings.MaxRetries)
                            {
                                _logger.LogDebug("Retrying connection in {Delay}s...", settings.RetryDelay.TotalSeconds);
                                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Retrying connection in {settings.RetryDelay.TotalSeconds}s..."));
                                await Task.Delay(settings.RetryDelay, cancellationToken);
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error waiting for pipe connection");
                        await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Error waiting for pipe connection: {ex.Message}"));
                    }

                    if (connected)
                    {
                        using var reader = new StreamReader(server);
                        Writer = new StreamWriter(server) { AutoFlush = true };

                        await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Подключение установлено. Готов к обмену данными."));

                        int messageCount = 0;
                        var startTime = DateTime.Now;

                        while (!cancellationToken.IsCancellationRequested && server.IsConnected)
                        {
                            try
                            {
                                var readTask = reader.ReadLineAsync();
                                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                                var completedTask = await Task.WhenAny(readTask, timeoutTask);
                                
                                if (completedTask == timeoutTask)
                                {
                                    _logger.LogWarning("Read timeout, checking connection...");
                                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Read timeout, checking connection..."));
                                    continue;
                                }
                                
                                var line = await readTask;
                                if (line == null) 
                                {
                                    _logger.LogInformation("Client disconnected");
                                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Client disconnected."));
                                    break;
                                }
                                
                                messageCount++;
                                
                                if (!string.IsNullOrWhiteSpace(line))
                                {
                                    _logger.LogDebug("MultiNamedPipeService received data: {Data}", line);
                                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Received: {line}"));
                                    
                                    // Send event for packet parsing with ProcessId
                                    await _eventBus.PublishAsync(new PipeDataReceivedEvent(line, "NamedPipe", ProcessId));
                                    
                                    // Show statistics every 100 messages
                                    if (messageCount % 100 == 0)
                                    {
                                        var elapsed = DateTime.Now - startTime;
                                        _logger.LogDebug("Statistics: Total={MessageCount}, Time={Elapsed}s", messageCount, elapsed.TotalSeconds);
                                        await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Statistics: Total={messageCount}, Time={elapsed.TotalSeconds:F1}s"));
                                    }
                                }
                            }
                            catch (IOException ex)
                            {
                                _logger.LogWarning(ex, "Connection broken by client");
                                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Connection broken by client."));
                                break;
                            }
                            catch (ObjectDisposedException ex)
                            {
                                _logger.LogWarning(ex, "Pipe was closed");
                                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Pipe was closed."));
                                break;
                            }
                            catch (OperationCanceledException ex)
                            {
                                _logger.LogInformation(ex, "Operation cancelled");
                                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Operation cancelled."));
                                break;
                            }
                            catch (SocketException ex)
                            {
                                _logger.LogError(ex, "Network error in NamedPipe communication");
                                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Network error: {ex.Message}"));
                                break;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Unexpected error in pipe communication");
                                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Unexpected error: {ex.Message}"));
                                
                                // Check for specific error code 109
                                if (ex.Message.Contains("109") || ex.Message.Contains("Failed to get the result of the asynchronous operation"))
                                {
                                    _logger.LogError("Error code 109 detected - this is likely a cancellation token issue");
                                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Error code 109 detected - this is likely a cancellation token issue"));
                                    
                                    // Check if cancellation was requested
                                    if (cancellationToken.IsCancellationRequested)
                                    {
                                        _logger.LogInformation("Cancellation was requested, stopping server");
                                        await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Cancellation was requested, stopping server"));
                                        break;
                                    }
                                    
                                    // Try to continue instead of breaking
                                    await Task.Delay(1000, cancellationToken);
                                    continue;
                                }
                                
                                break;
                            }
                        }
                        
                        // Final statistics
                        var totalElapsed = DateTime.Now - startTime;
                        _logger.LogInformation("Connection closed. Final statistics: Total={MessageCount}, Time={Elapsed}s", messageCount, totalElapsed.TotalSeconds);
                        await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Connection closed. Final statistics: Total={messageCount}, Time={totalElapsed.TotalSeconds:F1}s"));
                        
                        Writer = null;
                        IsConnected = false;
                        await _eventBus.PublishAsync(new ConnectionStatusChangedEvent(false, PipeName));
                    }
                }
                catch (OperationCanceledException ex)
                {
                    _logger.LogInformation(ex, "Operation cancelled");
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Operation cancelled."));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in main loop");
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MultiNamedPipeService] Error in main loop: {ex.Message}"));
                }
            }

            public void Stop()
            {
                try
                {
                    IsConnected = false;
                    Writer?.Dispose();
                    Writer = null;
                    _retryCount = 0;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error stopping pipe connection");
                }
            }
        }
    }
}

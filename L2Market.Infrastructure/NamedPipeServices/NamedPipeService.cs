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
    /// Service for working with Named Pipes
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class NamedPipeService : INamedPipeService, IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<NamedPipeService> _logger;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _serverTask;
        private StreamWriter? _writer;
        private bool _isConnected;
        private int _retryCount = 0;

        public bool IsConnected => _isConnected;

        public NamedPipeService(
            IEventBus eventBus, 
            IConfigurationService configurationService,
            ILogger<NamedPipeService> logger)
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
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[TEST] NamedPipeService starting - EventBus test"));
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Starting server for PID: {processId}"));
                
                // Check if server is already running for this process
                if (_serverTask != null && !_serverTask.IsCompleted)
                {
                    _logger.LogWarning("NamedPipe server is already running for PID: {ProcessId}", processId);
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Server already running for PID: {processId}"));
                    return;
                }
                
                // Stop any existing server before starting new one
                if (_serverTask != null)
                {
                    _logger.LogDebug("Stopping existing server before starting new one for PID: {ProcessId}", processId);
                    StopServer();
                    await Task.Delay(100); // Small delay to ensure cleanup
                }
                
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                
                // Add a timeout to prevent premature cancellation
                _cancellationTokenSource.CancelAfter(TimeSpan.FromHours(24)); // 24 hour timeout
                
                var pipeName = $"{processId}";
                
                _logger.LogDebug("Creating pipe: {PipeName}", pipeName);
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Creating pipe: {pipeName}"));
                
                // Start server in background - DON'T await it!
                _serverTask = Task.Run(() => ServerLoopAsync(pipeName, _cancellationTokenSource.Token));
                
                _logger.LogInformation("NamedPipe server started successfully (running in background)");
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Server started successfully (running in background)"));
                
                // Give server a moment to initialize
                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start NamedPipe server");
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Error starting server: {ex.Message}"));
                throw;
            }
        }

        public void StopServer()
        {
            try
            {
                var settings = _configurationService.Settings.NamedPipe;
                _logger.LogInformation("Stopping NamedPipe server");
                
                _cancellationTokenSource?.Cancel();
                _isConnected = false;
                _writer?.Dispose();
                _writer = null;
                _retryCount = 0; // Reset retry counter
                
                // Wait for background task completion with timeout
                if (_serverTask != null && !_serverTask.IsCompleted)
                {
                    try
                    {
                        _serverTask.Wait(settings.ServerShutdownTimeout);
                    }
                    catch (AggregateException ex) when (ex.InnerException is OperationCanceledException)
                    {
                        // Expected exception during cancellation
                        _logger.LogDebug("Server task cancelled as expected");
                    }
                    catch (TimeoutException)
                    {
                        // Force terminate task if it didn't complete in time
                        _logger.LogWarning("NamedPipe server task did not complete in time, forcing termination");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Exception while waiting for server task completion");
                    }
                }
                
                _logger.LogInformation("NamedPipe server stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping NamedPipe server");
            }
            finally
            {
                // Ensure cleanup
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                _serverTask = null;
            }
        }

        public async Task RestartServerAsync(uint processId, CancellationToken cancellationToken = default)
        {
            try
            {
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Restarting server for PID: {processId}"));
                StopServer();
                await Task.Delay(1000, cancellationToken); // Small delay before restart
                await StartServerAsync(processId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restarting server");
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Error restarting server: {ex.Message}"));
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
                
                if (_writer != null)
                {
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Отправка команды: {hex.Length} символов"));
                    await _writer.WriteLineAsync(jsonString);
                    await _writer.FlushAsync();
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Команда отправлена успешно"));
                }
                else
                {
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Ошибка: Пайп не подключен. Команда не отправлена."));
                }
            }
            catch (Exception ex)
            {
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Ошибка отправки команды: {ex.Message}"));
            }
        }

        private async Task ServerLoopAsync(string pipeName, CancellationToken ct)
        {
            try
            {
                var settings = _configurationService.Settings.NamedPipe;
                _logger.LogInformation("Starting main loop for pipe: {PipeName} with buffer size: {BufferSize}", pipeName, settings.BufferSize);
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Starting main loop for pipe: {pipeName} with buffer size: {settings.BufferSize} bytes"));
                
                using var server = new NamedPipeServerStream(
                    pipeName,
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous,
                    settings.BufferSize,
                    settings.BufferSize
                );

                _logger.LogDebug("Waiting for client connection to pipe {PipeName} (timeout: {Timeout}s)...", pipeName, settings.ConnectionTimeout.TotalSeconds);
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Waiting for client connection to pipe {pipeName} (timeout: {settings.ConnectionTimeout.TotalSeconds}s)..."));
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Attempt #{_retryCount + 1} of {settings.MaxRetries}"));
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[DEBUG] NamedPipe server created and waiting for connection..."));
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[DEBUG] Pipe name: '{pipeName}', Timeout: {settings.ConnectionTimeout.TotalSeconds}s"));

                bool connected = false;
                try
                {
                    var connectTask = server.WaitForConnectionAsync(ct);
                    var timeoutTask = Task.Delay(settings.ConnectionTimeout, ct);
                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                    
                    if (completedTask == connectTask && server.IsConnected)
                    {
                        connected = true;
                        _isConnected = true;
                        _retryCount = 0; // Reset retry counter on successful connection
                        _logger.LogInformation("Client connected to pipe {PipeName}", pipeName);
                        await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Client connected to pipe {pipeName}."));
                        await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[DEBUG] DLL successfully connected to NamedPipe!"));
                        await _eventBus.PublishAsync(new ConnectionStatusChangedEvent(true, pipeName));
                    }
                    else
                    {
                        _retryCount++;
                        _logger.LogWarning("Client connection timeout ({Timeout}s). Attempt #{Attempt} of {MaxRetries}", 
                            settings.ConnectionTimeout.TotalSeconds, _retryCount, settings.MaxRetries);
                        await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Client connection timeout ({settings.ConnectionTimeout.TotalSeconds}s). Attempt #{_retryCount} of {settings.MaxRetries}"));
                        await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[DEBUG] DLL did not connect to NamedPipe! Check if DLL is configured to connect to pipe '{pipeName}'"));
                        
                        if (_retryCount < settings.MaxRetries)
                        {
                            _logger.LogDebug("Retrying connection in {Delay}s...", settings.RetryDelay.TotalSeconds);
                            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Retrying connection in {settings.RetryDelay.TotalSeconds}s..."));
                            await Task.Delay(settings.RetryDelay, ct);
                            return; // Exit method for retry
                        }
                        else
                        {
                            _logger.LogError("Reached maximum connection attempts. Closing pipe.");
                            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Reached maximum connection attempts. Closing pipe."));
                            _retryCount = 0; // Reset counter
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error waiting for pipe connection");
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Error waiting for pipe connection: {ex.Message}"));
                }

                if (connected)
                {
                    using var reader = new StreamReader(server);
                    _writer = new StreamWriter(server) { AutoFlush = true };

                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Подключение установлено. Готов к обмену данными."));
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] CancellationToken.IsCancellationRequested: {ct.IsCancellationRequested}"));
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Server.IsConnected: {server.IsConnected}"));

                    int messageCount = 0;
                    var startTime = DateTime.Now;

                    while (!ct.IsCancellationRequested && server.IsConnected)
                    {
                        try
                        {
                            // Use ReadLineAsync with proper cancellation handling
                            var readTask = reader.ReadLineAsync();
                            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), ct);
                            var completedTask = await Task.WhenAny(readTask, timeoutTask);
                            
                            if (completedTask == timeoutTask)
                            {
                                _logger.LogWarning("Read timeout, checking connection...");
                                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Read timeout, checking connection..."));
                                continue;
                            }
                            
                            var line = await readTask;
                            if (line == null) 
                            {
                                _logger.LogInformation("Client disconnected");
                                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Client disconnected."));
                                break;
                            }
                            
                            messageCount++;
                            
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                _logger.LogDebug("NamedPipeService received data: {Data}", line);
                                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Received: {line}"));
                                
                                // Send event for packet parsing
                                await _eventBus.PublishAsync(new PipeDataReceivedEvent(line));
                                
                                // Show statistics every 100 messages
                                if (messageCount % 100 == 0)
                                {
                                    var elapsed = DateTime.Now - startTime;
                                    _logger.LogDebug("Statistics: Total={MessageCount}, Time={Elapsed}s", messageCount, elapsed.TotalSeconds);
                                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Statistics: Total={messageCount}, Time={elapsed.TotalSeconds:F1}s"));
                                }
                            }
                        }
                        catch (IOException ex)
                        {
                            _logger.LogWarning(ex, "Connection broken by client");
                            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Connection broken by client."));
                            break;
                        }
                        catch (ObjectDisposedException ex)
                        {
                            _logger.LogWarning(ex, "Pipe was closed");
                            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Pipe was closed."));
                            break;
                        }
                        catch (OperationCanceledException ex)
                        {
                            _logger.LogInformation(ex, "Operation cancelled");
                            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Operation cancelled."));
                            break;
                        }
                        catch (SocketException ex)
                        {
                            _logger.LogError(ex, "Network error in NamedPipe communication");
                            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Network error: {ex.Message}"));
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Unexpected error in pipe communication");
                            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Unexpected error: {ex.Message}"));
                            
                            // Check for specific error code 109
                            if (ex.Message.Contains("109") || ex.Message.Contains("Failed to get the result of the asynchronous operation"))
                            {
                                _logger.LogError("Error code 109 detected - this is likely a cancellation token issue");
                                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Error code 109 detected - this is likely a cancellation token issue"));
                                
                                // Check if cancellation was requested
                                if (ct.IsCancellationRequested)
                                {
                                    _logger.LogInformation("Cancellation was requested, stopping server");
                                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Cancellation was requested, stopping server"));
                                    break;
                                }
                                
                                // Try to continue instead of breaking
                                await Task.Delay(1000, ct);
                                continue;
                            }
                            
                            break;
                        }
                    }
                    
                    // Final statistics
                    var totalElapsed = DateTime.Now - startTime;
                    _logger.LogInformation("Connection closed. Final statistics: Total={MessageCount}, Time={Elapsed}s", messageCount, totalElapsed.TotalSeconds);
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Connection closed. Final statistics: Total={messageCount}, Time={totalElapsed.TotalSeconds:F1}s"));
                    
                    _writer = null;
                    _isConnected = false;
                    await _eventBus.PublishAsync(new ConnectionStatusChangedEvent(false, pipeName));
                    
                    // Notify about reconnection requirement
                    _logger.LogInformation("Reconnection required for pipe {PipeName}", pipeName);
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Reconnection required for pipe {pipeName}"));
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "Operation cancelled");
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Operation cancelled."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in main loop");
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NamedPipeService] Error in main loop: {ex.Message}"));
            }
        }

        public void Dispose()
        {
            try
            {
                StopServer();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Dispose: {ex.Message}");
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _writer?.Dispose();
            }
        }

        public Task SendCommandAsync(string hex, uint processId)
        {
            throw new NotImplementedException();
        }
    }
}

using L2Market.Core;
using L2Market.Core.Configuration;
using L2Market.Domain.Common;
using L2Market.Domain.Events;
using L2Market.Domain.Services;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace L2Market.Core.Services
{

    /// <summary>
    /// Application service implementation
    /// </summary>
    public class ApplicationService : IApplicationService
    {
        private readonly IDllInjectionService _dllInjectionService;
        private readonly INamedPipeService _namedPipeService;
        private readonly IEventBus _eventBus;
        private readonly IConfigurationService _configurationService;
        private readonly IPacketParserService _packetParserService;
        private readonly ILogger<ApplicationService> _logger;

        public ApplicationService(
            IDllInjectionService dllInjectionService,
            INamedPipeService namedPipeService,
            IEventBus eventBus,
            IConfigurationService configurationService,
            IPacketParserService packetParserService,
            ILogger<ApplicationService> logger)
        {
            _dllInjectionService = dllInjectionService ?? throw new ArgumentNullException(nameof(dllInjectionService));
            _namedPipeService = namedPipeService ?? throw new ArgumentNullException(nameof(namedPipeService));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _packetParserService = packetParserService ?? throw new ArgumentNullException(nameof(packetParserService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<WorkflowResult> ExecuteInjectionWorkflowAsync(string dllPath, string processName, CancellationToken cancellationToken = default)
        {
            var settings = _configurationService.Settings;
            var startTime = DateTime.UtcNow;
            var result = new WorkflowResult();

            try
            {
                _logger.LogInformation("Starting injection workflow for process: {ProcessName}, DLL: {DllPath}", processName, dllPath);
                
                // Test EventBus immediately
                await _eventBus.PublishAsync(new LogMessageReceivedEvent("[TEST] EventBus test at start of workflow"));
                await _eventBus.PublishAsync(new LogMessageReceivedEvent("[TEST] Testing UI event handler subscription"));
                
                // Publish workflow started event
                await _eventBus.PublishAsync(new WorkflowStartedEvent
                {
                    DllPath = dllPath,
                    ProcessName = processName
                });

                // Validate inputs
                if (!ValidateInputs(dllPath, processName, out string validationError))
                {
                    result.ErrorMessage = validationError;
                    _logger.LogWarning("Input validation failed: {Error}", validationError);
                    await _eventBus.PublishAsync(new WorkflowFailedEvent
                    {
                        DllPath = dllPath,
                        ProcessName = processName,
                        ErrorMessage = validationError,
                        Duration = DateTime.UtcNow - startTime
                    });
                    return result;
                }

                // Step 1: Process search
                await _eventBus.PublishAsync(new WorkflowStepStartedEvent
                {
                    StepName = "ProcessSearch",
                    Description = "Searching for process..."
                });
                var processResult = await _dllInjectionService.FindProcessAsync(processName);
                
                if (!processResult.Found)
                {
                    result.ErrorMessage = $"Process '{processName}' not found";
                    _logger.LogWarning("Process not found: {ProcessName}", processName);
                    await _eventBus.PublishAsync(new WorkflowFailedEvent
                    {
                        DllPath = dllPath,
                        ProcessName = processName,
                        ErrorMessage = result.ErrorMessage,
                        Duration = DateTime.UtcNow - startTime
                    });
                    return result;
                }

                result.ProcessId = processResult.ProcessId;
                result.ProcessName = processName;
                _logger.LogInformation("Process found: {ProcessName} (PID: {ProcessId})", processName, result.ProcessId);

                // Step 2: Create NamedPipe server (runs in background)
                await _eventBus.PublishAsync(new WorkflowStepStartedEvent
                {
                    StepName = "NamedPipeCreation",
                    Description = "Starting NamedPipe server..."
                });
                await _namedPipeService.StartServerAsync((uint)result.ProcessId, cancellationToken);

                // Step 2.5: Start packet parser service
                await _eventBus.PublishAsync(new WorkflowStepStartedEvent
                {
                    StepName = "PacketParserStart",
                    Description = "Starting packet parser service..."
                });
                await _packetParserService.StartAsync(cancellationToken);
                
                // Test EventBus with a test message
                await _eventBus.PublishAsync(new LogMessageReceivedEvent("[TEST] EventBus is working!"));
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[DEBUG] Process ID: {result.ProcessId}, Pipe name will be: '{result.ProcessId}'"));
                
                // Give NamedPipe server time to start
                await Task.Delay(1000, cancellationToken);

                // Step 3: DLL injection (DLL will connect to running NamedPipe)
                await _eventBus.PublishAsync(new WorkflowStepStartedEvent
                {
                    StepName = "DllInjection",
                    Description = "Injecting DLL..."
                });
                var injectionResult = await _dllInjectionService.InjectDllAsync(dllPath, result.ProcessId);
                
                if (!injectionResult.Success)
                {
                    result.ErrorMessage = injectionResult.ErrorMessage;
                    result.DllInjected = false;
                    _logger.LogError("DLL injection failed: {Error}", injectionResult.ErrorMessage);
                    await _eventBus.PublishAsync(new WorkflowFailedEvent
                    {
                        DllPath = dllPath,
                        ProcessName = processName,
                        ProcessId = result.ProcessId,
                        ErrorMessage = result.ErrorMessage,
                        Duration = DateTime.UtcNow - startTime
                    });
                    return result;
                }

                result.DllInjected = true;
                _logger.LogInformation("DLL injected successfully");

                // Step 4: Wait for DLL to connect to NamedPipe
                await _eventBus.PublishAsync(new WorkflowStepStartedEvent
                {
                    StepName = "NamedPipeConnection",
                    Description = "Waiting for DLL to connect to NamedPipe..."
                });
                var connectionTimeout = settings.NamedPipe.ConnectionTimeout;
                var connectionResult = await WaitForNamedPipeConnectionAsync(connectionTimeout, cancellationToken);
                
                if (!connectionResult)
                {
                    result.ErrorMessage = $"DLL did not connect to NamedPipe within {connectionTimeout.TotalSeconds}s timeout";
                    result.NamedPipeConnected = false;
                    _logger.LogWarning("DLL connection timeout after {Timeout}s", connectionTimeout.TotalSeconds);
                    await _eventBus.PublishAsync(new WorkflowFailedEvent
                    {
                        DllPath = dllPath,
                        ProcessName = processName,
                        ProcessId = result.ProcessId,
                        ErrorMessage = result.ErrorMessage,
                        Duration = DateTime.UtcNow - startTime
                    });
                    return result;
                }

                result.NamedPipeConnected = true;
                result.Success = true;
                result.TotalDuration = DateTime.UtcNow - startTime;

                _logger.LogInformation("Workflow completed successfully in {Duration}s", result.TotalDuration.TotalSeconds);

                // Publish workflow completed event
                await _eventBus.PublishAsync(new WorkflowCompletedEvent
                {
                    DllPath = dllPath,
                    ProcessName = processName,
                    ProcessId = result.ProcessId,
                    Duration = result.TotalDuration
                });

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                result.TotalDuration = DateTime.UtcNow - startTime;
                
                _logger.LogError(ex, "Workflow failed with exception");
                
                // Stop packet parser service on error
                try
                {
                    _packetParserService.Stop();
                }
                catch (Exception stopEx)
                {
                    _logger.LogWarning(stopEx, "Error stopping packet parser service");
                }
                
                await _eventBus.PublishAsync(new WorkflowFailedEvent
                {
                    DllPath = dllPath,
                    ProcessName = processName,
                    ProcessId = result.ProcessId,
                    ErrorMessage = ex.Message,
                    Duration = result.TotalDuration
                });
                
                return result;
            }
        }

        public async Task<bool> SendCommandAsync(string hexCommand, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_namedPipeService.IsConnected)
                {
                    _logger.LogWarning("NamedPipe not connected. Cannot send command.");
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent("NamedPipe not connected. Cannot send command."));
                    return false;
                }

                _logger.LogDebug("Sending command: {Command}", hexCommand);
                await _eventBus.PublishAsync(new CommandSendingEvent { HexCommand = hexCommand });
                await _namedPipeService.SendCommandAsync(hexCommand);
                await _eventBus.PublishAsync(new CommandSentEvent { HexCommand = hexCommand });
                
                _logger.LogInformation("Command sent successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send command: {Command}", hexCommand);
                await _eventBus.PublishAsync(new CommandFailedEvent 
                { 
                    HexCommand = hexCommand, 
                    ErrorMessage = ex.Message 
                });
                return false;
            }
        }

        private bool ValidateInputs(string dllPath, string processName, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(dllPath))
            {
                errorMessage = "DLL path is required";
                return false;
            }

            if (string.IsNullOrWhiteSpace(processName))
            {
                errorMessage = "Process name is required";
                return false;
            }

            if (!File.Exists(dllPath))
            {
                errorMessage = $"DLL file not found: {dllPath}";
                return false;
            }

            return true;
        }

        private async Task<bool> WaitForNamedPipeConnectionAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;
            var checkInterval = TimeSpan.FromMilliseconds(100);
            
            _logger.LogDebug("Waiting for NamedPipe connection with timeout: {Timeout}s", timeout.TotalSeconds);
            
            while (!_namedPipeService.IsConnected && DateTime.UtcNow - startTime < timeout)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogDebug("Connection wait cancelled");
                    return false;
                }
                    
                await Task.Delay(checkInterval, cancellationToken);
            }
            
            var connected = _namedPipeService.IsConnected;
            _logger.LogDebug("NamedPipe connection result: {Connected}", connected);
            return connected;
        }
    }
}

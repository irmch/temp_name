using L2Market.Domain;
using L2Market.Domain.Common;
using L2Market.Domain.Events;
using L2Market.Domain.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace L2Market.Infrastructure.Services
{
    /// <summary>
    /// DLL injection service implementation
    /// </summary>
    public class DllInjectionService : IDllInjectionService
    {
        private readonly IDllInjector _dllInjector;
        private readonly IEventBus _eventBus;
        private readonly ILogger<DllInjectionService> _logger;

        public DllInjectionService(IDllInjector dllInjector, IEventBus eventBus, ILogger<DllInjectionService> logger)
        {
            _dllInjector = dllInjector ?? throw new ArgumentNullException(nameof(dllInjector));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<InjectionResult> InjectDllAsync(string dllPath, int processId)
        {
            var startTime = DateTime.UtcNow;
            
            // Публикуем событие начала инжекции
            await _eventBus.PublishAsync(new DllInjectionStartedEvent
            {
                DllPath = dllPath,
                ProcessId = processId
            });

            try
            {
                _logger.LogInformation("Starting DLL injection: {DllPath} into process {ProcessId}", dllPath, processId);
                
                var result = _dllInjector.InjectDll(dllPath, processId);
                
                var duration = DateTime.UtcNow - startTime;
                
                if (result.Success)
                {
                    _logger.LogInformation("DLL injection completed successfully in {Duration}ms", duration.TotalMilliseconds);
                    
                    await _eventBus.PublishAsync(new DllInjectionCompletedEvent
                    {
                        DllPath = dllPath,
                        ProcessId = processId,
                        Duration = duration
                    });
                }
                else
                {
                    _logger.LogError("DLL injection failed: {ErrorMessage}", result.ErrorMessage);
                    
                    await _eventBus.PublishAsync(new DllInjectionFailedEvent
                    {
                        DllPath = dllPath,
                        ProcessId = processId,
                        ErrorMessage = result.ErrorMessage,
                        Duration = duration
                    });
                }
                
                return result;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "Unexpected error during DLL injection");
                
                await _eventBus.PublishAsync(new DllInjectionFailedEvent
                {
                    DllPath = dllPath,
                    ProcessId = processId,
                    ErrorMessage = ex.Message,
                    Duration = duration
                });
                
                return new InjectionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ProcessId = processId
                };
            }
        }

        public async Task<ProcessSearchResult> FindProcessAsync(string processName)
        {
            _logger.LogInformation("Searching for process: {ProcessName}", processName);
            
            await _eventBus.PublishAsync(new ProcessSearchStartedEvent
            {
                ProcessName = processName
            });

            try
            {
                var result = _dllInjector.FindProcessByName(processName);
                
                if (result.Found)
                {
                    _logger.LogInformation("Process found: {ProcessName} (PID: {ProcessId})", result.ProcessName, result.ProcessId);
                    
                    await _eventBus.PublishAsync(new ProcessFoundEvent
                    {
                        ProcessId = result.ProcessId,
                        ProcessName = result.ProcessName
                    });
                }
                else
                {
                    _logger.LogWarning("Process not found: {ProcessName}", processName);
                    
                    await _eventBus.PublishAsync(new ProcessNotFoundEvent
                    {
                        ProcessName = processName,
                        ErrorMessage = result.ErrorMessage
                    });
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for process: {ProcessName}", processName);
                
                await _eventBus.PublishAsync(new ProcessNotFoundEvent
                {
                    ProcessName = processName,
                    ErrorMessage = ex.Message
                });
                
                return new ProcessSearchResult
                {
                    Found = false,
                    ProcessName = processName,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}

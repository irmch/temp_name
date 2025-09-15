using L2Market.Domain.Events;
using L2Market.Domain.Common;
using L2Market.UI.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace L2Market.UI.EventHandlers
{
    /// <summary>
    /// Event handler for UI updates - uses subscriptions instead of DI
    /// </summary>
    public class UiEventHandler
    {
        private readonly ILogger<UiEventHandler> _logger;
        private readonly IEventBus _eventBus;
        private readonly LogsViewModel _logsViewModel;

        public UiEventHandler(ILogger<UiEventHandler> logger, IEventBus eventBus, LogsViewModel logsViewModel)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logsViewModel = logsViewModel ?? throw new ArgumentNullException(nameof(logsViewModel));
            
            System.Diagnostics.Debug.WriteLine("[DEBUG] UiEventHandler: Constructor called - subscribing to events");
            
            // Subscribe to all events
            SubscribeToEvents();
            
            // Add test message to verify UiEventHandler is working
            _logsViewModel.AddLogEntry("🔧 UiEventHandler initialized and subscribed to events", "Information");
            
            // Test EventBus directly
            _ = Task.Run(async () =>
            {
                await Task.Delay(1000); // Wait 1 second
                await _eventBus.PublishAsync(new LogMessageReceivedEvent("[TEST] UiEventHandler: Direct EventBus test message"));
            });
            
            System.Diagnostics.Debug.WriteLine("[DEBUG] UiEventHandler: Constructor completed - all subscriptions done");
        }

        private void SubscribeToEvents()
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG] UiEventHandler: Starting subscriptions...");
            
            _eventBus.Subscribe<DllInjectionStartedEvent>(Handle);
            _eventBus.Subscribe<DllInjectionCompletedEvent>(Handle);
            _eventBus.Subscribe<DllInjectionFailedEvent>(Handle);
            _eventBus.Subscribe<ProcessSearchStartedEvent>(Handle);
            _eventBus.Subscribe<ProcessFoundEvent>(Handle);
            _eventBus.Subscribe<ProcessNotFoundEvent>(Handle);
            _eventBus.Subscribe<WorkflowStartedEvent>(Handle);
            _eventBus.Subscribe<WorkflowCompletedEvent>(Handle);
            _eventBus.Subscribe<WorkflowFailedEvent>(Handle);
            _eventBus.Subscribe<WorkflowStepStartedEvent>(Handle);
            _eventBus.Subscribe<CommandSendingEvent>(Handle);
            _eventBus.Subscribe<CommandSentEvent>(Handle);
            _eventBus.Subscribe<CommandFailedEvent>(Handle);
            _eventBus.Subscribe<PipeDataReceivedEvent>(Handle);
            _eventBus.Subscribe<LogMessageReceivedEvent>(Handle);
            
            System.Diagnostics.Debug.WriteLine("[DEBUG] UiEventHandler: All subscriptions completed");
        }

        public Task Handle(DllInjectionStartedEvent domainEvent)
        {
            try
            {
                _logsViewModel.AddLogEntry($"🚀 DLL injection started for process ID: {domainEvent.ProcessId}", "Information");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UI ERROR] Error updating UI for DllInjectionStartedEvent: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        public Task Handle(DllInjectionCompletedEvent domainEvent)
        {
            try
            {
                _logsViewModel.AddLogEntry($"✅ DLL injection completed for process: {domainEvent.ProcessName}", "Information");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UI ERROR] Error updating UI for DllInjectionCompletedEvent: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        public Task Handle(DllInjectionFailedEvent domainEvent)
        {
            try
            {
                _logsViewModel.AddLogEntry($"❌ DLL injection failed for process ID: {domainEvent.ProcessId}. Error: {domainEvent.ErrorMessage}", "Error");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UI ERROR] Error updating UI for DllInjectionFailedEvent: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        public Task Handle(ProcessSearchStartedEvent domainEvent)
        {
            try
            {
                _logsViewModel.AddLogEntry($"🔍 Searching for process: {domainEvent.ProcessName}", "Information");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UI ERROR] Error updating UI for ProcessSearchStartedEvent: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        public Task Handle(ProcessFoundEvent domainEvent)
        {
            try
            {
                _logsViewModel.AddLogEntry($"✅ Process found: {domainEvent.ProcessName} (PID: {domainEvent.ProcessId})", "Information");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UI ERROR] Error updating UI for ProcessFoundEvent: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        public Task Handle(ProcessNotFoundEvent domainEvent)
        {
            try
            {
                _logsViewModel.AddLogEntry($"❌ Process not found: {domainEvent.ProcessName}", "Warning");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UI ERROR] Error updating UI for ProcessNotFoundEvent: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        public Task Handle(WorkflowStartedEvent domainEvent)
        {
            try
            {
                _logsViewModel.AddLogEntry($"🔄 Workflow started for process: {domainEvent.ProcessName} (PID: {domainEvent.ProcessId})", "Information");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UI ERROR] Error updating UI for WorkflowStartedEvent: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        public Task Handle(WorkflowCompletedEvent domainEvent)
        {
            try
            {
                _logsViewModel.AddLogEntry($"✅ Workflow completed for process: {domainEvent.ProcessName} (PID: {domainEvent.ProcessId}) in {domainEvent.Duration.TotalSeconds:F1}s", "Information");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UI ERROR] Error updating UI for WorkflowCompletedEvent: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        public Task Handle(WorkflowFailedEvent domainEvent)
        {
            try
            {
                _logsViewModel.AddLogEntry($"❌ Workflow failed for process: {domainEvent.ProcessName} (PID: {domainEvent.ProcessId}). Error: {domainEvent.ErrorMessage}", "Error");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UI ERROR] Error updating UI for WorkflowFailedEvent: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        public Task Handle(WorkflowStepStartedEvent domainEvent)
        {
            try
            {
                _logsViewModel.AddLogEntry($"⏳ {domainEvent.StepName}: {domainEvent.Description}", "Information");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UI ERROR] Error updating UI for WorkflowStepStartedEvent: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        public Task Handle(CommandSendingEvent domainEvent)
        {
            try
            {
                _logsViewModel.AddLogEntry($"📤 Sending command: {domainEvent.HexCommand}", "Information");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UI ERROR] Error updating UI for CommandSendingEvent: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        public Task Handle(CommandSentEvent domainEvent)
        {
            try
            {
                _logsViewModel.AddLogEntry($"✅ Command sent successfully: {domainEvent.HexCommand}", "Information");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UI ERROR] Error updating UI for CommandSentEvent: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        public Task Handle(CommandFailedEvent domainEvent)
        {
            try
            {
                _logsViewModel.AddLogEntry($"❌ Command failed: {domainEvent.HexCommand}. Error: {domainEvent.ErrorMessage}", "Error");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UI ERROR] Error updating UI for CommandFailedEvent: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        public Task Handle(PipeDataReceivedEvent domainEvent)
        {
            try
            {
                // Pipe data received - no UI logging needed
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UI ERROR] Error updating UI for PipeDataReceivedEvent: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        public Task Handle(LogMessageReceivedEvent domainEvent)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] UiEventHandler: Received LogMessageReceivedEvent: {domainEvent.Message}");
                
                // Send to LogsViewModel
                _logsViewModel.AddLogEntry(domainEvent.Message, "Information");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] UiEventHandler: Message sent to LogsViewModel: {domainEvent.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UI ERROR] Error updating UI: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[UI ERROR] Exception details: {ex}");
            }
            return Task.CompletedTask;
        }
    }
}
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

        public UiEventHandler(ILogger<UiEventHandler> logger, IEventBus eventBus)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            
            System.Diagnostics.Debug.WriteLine("[DEBUG] UiEventHandler: Constructor called - subscribing to events");
            
            // Subscribe to all events
            SubscribeToEvents();
            
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
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow?.DataContext is MainViewModel viewModel)
                    {
                        viewModel.LogMessage($"🚀 DLL injection started for process ID: {domainEvent.ProcessId}");
                    }
                });
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
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow?.DataContext is MainViewModel viewModel)
                    {
                        viewModel.LogMessage($"✅ DLL injection completed for process: {domainEvent.ProcessName}");
                    }
                });
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
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow?.DataContext is MainViewModel viewModel)
                    {
                        viewModel.LogMessage($"❌ DLL injection failed for process ID: {domainEvent.ProcessId}. Error: {domainEvent.ErrorMessage}");
                    }
                });
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
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow?.DataContext is MainViewModel viewModel)
                    {
                        viewModel.LogMessage($"🔍 Searching for process: {domainEvent.ProcessName}");
                    }
                });
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
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow?.DataContext is MainViewModel viewModel)
                    {
                        viewModel.LogMessage($"✅ Process found: {domainEvent.ProcessName} (PID: {domainEvent.ProcessId})");
                    }
                });
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
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow?.DataContext is MainViewModel viewModel)
                    {
                        viewModel.LogMessage($"❌ Process not found: {domainEvent.ProcessName}");
                    }
                });
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
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow?.DataContext is MainViewModel viewModel)
                    {
                        viewModel.LogMessage($"🔄 Workflow started for process: {domainEvent.ProcessName}");
                    }
                });
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
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow?.DataContext is MainViewModel viewModel)
                    {
                        viewModel.LogMessage($"✅ Workflow completed for process: {domainEvent.ProcessName}");
                    }
                });
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
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow?.DataContext is MainViewModel viewModel)
                    {
                        viewModel.LogMessage($"❌ Workflow failed for process: {domainEvent.ProcessName}. Error: {domainEvent.ErrorMessage}");
                    }
                });
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
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow?.DataContext is MainViewModel viewModel)
                    {
                        viewModel.LogMessage($"⏳ {domainEvent.StepName}: {domainEvent.Description}");
                    }
                });
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
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow?.DataContext is MainViewModel viewModel)
                    {
                        viewModel.LogMessage($"📤 Sending command: {domainEvent.HexCommand}");
                    }
                });
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
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow?.DataContext is MainViewModel viewModel)
                    {
                        viewModel.LogMessage($"✅ Command sent successfully: {domainEvent.HexCommand}");
                    }
                });
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
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow?.DataContext is MainViewModel viewModel)
                    {
                        viewModel.LogMessage($"❌ Command failed: {domainEvent.HexCommand}. Error: {domainEvent.ErrorMessage}");
                    }
                });
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
                
                // Use Dispatcher to ensure we're on the UI thread
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow?.DataContext is MainViewModel viewModel)
                    {
                        viewModel.LogMessage(domainEvent.Message);
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] UiEventHandler: Message sent to UI: {domainEvent.Message}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[UI ERROR] MainWindow or ViewModel is null. MainWindow: {mainWindow != null}, DataContext: {mainWindow?.DataContext != null}");
                    }
                });
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
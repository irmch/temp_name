using System;
using System.Threading;
using System.Threading.Tasks;

namespace L2Market.Domain.Services
{
    /// <summary>
    /// Сервис приложения для координации бизнес-логики
    /// </summary>
    public interface IApplicationService
    {
        /// <summary>
        /// Выполняет полный workflow: поиск процесса + создание pipe + инжекция
        /// </summary>
        Task<WorkflowResult> ExecuteInjectionWorkflowAsync(string dllPath, string processName, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Выполняет автоматическую инъекцию: ищет процесс, ждет окно, инжектирует DLL
        /// </summary>
        Task<WorkflowResult> ExecuteAutomaticInjectionAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Отправляет команду через NamedPipe
        /// </summary>
        Task<bool> SendCommandAsync(string hexCommand, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Результат выполнения workflow
    /// </summary>
    public class WorkflowResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public bool NamedPipeConnected { get; set; }
        public bool DllInjected { get; set; }
        public TimeSpan TotalDuration { get; set; }
    }
}

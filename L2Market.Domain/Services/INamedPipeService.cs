using System.Threading;
using System.Threading.Tasks;

namespace L2Market.Domain.Services
{
    /// <summary>
    /// Интерфейс для работы с Named Pipe сервисом
    /// </summary>
    public interface INamedPipeService : IDisposable
    {
        /// <summary>
        /// Получает статус подключения
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Запускает сервер Named Pipe для указанного процесса
        /// </summary>
        /// <param name="processId">ID процесса</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Задача запуска сервера</returns>
        Task StartServerAsync(uint processId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Останавливает сервер Named Pipe
        /// </summary>
        void StopServer();

        /// <summary>
        /// Перезапускает сервер Named Pipe
        /// </summary>
        /// <param name="processId">ID процесса</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Задача перезапуска сервера</returns>
        Task RestartServerAsync(uint processId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Отправляет команду через Named Pipe
        /// </summary>
        /// <param name="hex">Hex данные для отправки</param>
        /// <returns>Задача отправки команды</returns>
        Task SendCommandAsync(string hex);

        /// <summary>
        /// Отправляет команду через Named Pipe в конкретный процесс
        /// </summary>
        /// <param name="hex">Hex данные для отправки</param>
        /// <param name="processId">ID процесса</param>
        /// <returns>Задача отправки команды</returns>
        Task SendCommandAsync(string hex, uint processId);
    }
}

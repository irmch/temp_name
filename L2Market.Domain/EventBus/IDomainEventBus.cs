using System.Threading;
using System.Threading.Tasks;

namespace L2Market.Domain.EventBus
{
    /// <summary>
    /// Интерфейс для шины доменных событий
    /// </summary>
    public interface IDomainEventBus
    {
        /// <summary>
        /// Публикует доменное событие
        /// </summary>
        /// <typeparam name="TEvent">Тип события</typeparam>
        /// <param name="domainEvent">Событие для публикации</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Задача публикации события</returns>
        Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) where TEvent : class;
        
        /// <summary>
        /// Подписывается на доменное событие
        /// </summary>
        /// <typeparam name="TEvent">Тип события</typeparam>
        /// <param name="handler">Обработчик события</param>
        void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class;
    }
}

using System.Threading;
using System.Threading.Tasks;

namespace L2Market.Domain.EventBus
{
    /// <summary>
    /// Интерфейс для обработчиков доменных событий
    /// </summary>
    /// <typeparam name="TEvent">Тип события</typeparam>
    public interface IDomainEventHandler<in TEvent>
    {
        /// <summary>
        /// Обрабатывает доменное событие
        /// </summary>
        /// <param name="domainEvent">Событие для обработки</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Задача обработки события</returns>
        Task Handle(TEvent domainEvent, CancellationToken cancellationToken = default);
    }
}

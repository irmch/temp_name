using System;
using System.Threading.Tasks;
using L2Market.Domain.Events;
using L2Market.Domain.Common;
using L2Market.Core.Services;

namespace L2Market.Core.Services
{
    /// <summary>
    /// Сервис автовыкупа
    /// </summary>
    public class AutoBuyService
    {
        private readonly IEventBus _eventBus;
        private long _availableMoney = 10_000_000; // 10M по умолчанию

        public AutoBuyService(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        /// <summary>
        /// Доступные деньги для автовыкупа
        /// </summary>
        public long AvailableMoney
        {
            get => _availableMoney;
            set => _availableMoney = value;
        }

        /// <summary>
        /// Попытаться купить предмет автоматически
        /// </summary>
        public async Task<bool> TryAutoBuyAsync(ItemMatch match)
        {
            if (match?.Rule == null || match?.Item == null)
                return false;

            try
            {
                // Проверяем, можем ли позволить себе покупку
                if (!await CanAffordAsync(match.Item.Price))
                {
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[AutoBuy] Недостаточно денег для покупки {match.Item.ItemName} за {match.Item.FormattedPrice}"));
                    return false;
                }

                // Проверяем, доступен ли еще предмет
                if (!await IsItemStillAvailableAsync(match))
                {
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[AutoBuy] Предмет {match.Item.ItemName} больше не доступен"));
                    return false;
                }

                // Выполняем покупку
                var success = await ExecutePurchaseAsync(match);
                
                if (success)
                {
                    _availableMoney -= match.Item.Price;
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[AutoBuy] ✅ Успешно куплен {match.Item.ItemName} за {match.Item.FormattedPrice}"));
                }
                else
                {
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[AutoBuy] ❌ Не удалось купить {match.Item.ItemName}"));
                }

                return success;
            }
            catch (Exception ex)
            {
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[AutoBuy] Ошибка при покупке {match.Item.ItemName}: {ex.Message}"));
                return false;
            }
        }

        /// <summary>
        /// Проверить, хватает ли денег
        /// </summary>
        public async Task<bool> CanAffordAsync(long price)
        {
            await Task.CompletedTask; // Для будущих асинхронных операций
            return _availableMoney >= price;
        }

        /// <summary>
        /// Проверить, доступен ли еще предмет
        /// </summary>
        public async Task<bool> IsItemStillAvailableAsync(ItemMatch match)
        {
            // TODO: Реализовать проверку доступности предмета
            // Это может включать повторный запрос к серверу игры
            await Task.CompletedTask;
            return true; // Пока всегда возвращаем true
        }

        /// <summary>
        /// Выполнить покупку
        /// </summary>
        private async Task<bool> ExecutePurchaseAsync(ItemMatch match)
        {
            // TODO: Реализовать реальную логику покупки
            // Это может включать:
            // 1. Отправку команды в игру
            // 2. Ожидание подтверждения
            // 3. Проверку успешности операции
            
            await Task.Delay(1000); // Имитация времени покупки
            
            // Пока возвращаем случайный результат для демонстрации
            var random = new Random();
            return random.NextDouble() > 0.3; // 70% успеха
        }

        /// <summary>
        /// Добавить деньги
        /// </summary>
        public async Task AddMoneyAsync(long amount)
        {
            _availableMoney += amount;
            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[AutoBuy] Добавлено {amount:N0} денег. Всего: {_availableMoney:N0}"));
        }

        /// <summary>
        /// Получить форматированную сумму денег
        /// </summary>
        public string GetFormattedMoney()
        {
            if (_availableMoney >= 1_000_000_000)
                return $"{_availableMoney / 1_000_000_000.0:F1}B";
            if (_availableMoney >= 1_000_000)
                return $"{_availableMoney / 1_000_000.0:F1}M";
            if (_availableMoney >= 1_000)
                return $"{_availableMoney / 1_000.0:F1}K";
            return _availableMoney.ToString("N0");
        }
    }
}

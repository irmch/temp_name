using System;
using System.Threading.Tasks;
using L2Market.Domain.Events;
using L2Market.Domain.Common;
using L2Market.Core.Services;

namespace L2Market.Core.Services
{
    /// <summary>
    /// Сервис уведомлений
    /// </summary>
    public class NotificationService
    {
        private readonly IEventBus _eventBus;

        public NotificationService(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        /// <summary>
        /// Отправить уведомление о найденном предмете
        /// </summary>
        public async Task SendNotificationAsync(ItemMatch match)
        {
            if (match?.Rule == null || match?.Item == null) return;

            var rule = match.Rule;
            var item = match.Item;

            // Формируем сообщение
            var message = $"💰 Найден предмет по правилу '{rule.Name}'\n" +
                         $"📦 {item.ItemName} (+{item.EnchantLevel})\n" +
                         $"💰 Цена: {item.FormattedPrice}\n" +
                         $"🏪 {item.MarketType} | Продавец: {item.SellerName}\n" +
                         $"⏰ {match.FoundAt:HH:mm:ss}";

            // Логируем в приложение
            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NOTIFICATION] {message}"));

            // Воспроизводим звук
            if (rule.PlaySound)
            {
                await PlaySoundAsync();
            }

            // Отправляем в Discord (если настроено)
            if (rule.SendDiscord)
            {
                await SendDiscordMessageAsync(message);
            }

            // Отправляем в Telegram (если настроено)
            if (rule.SendTelegram)
            {
                await SendTelegramMessageAsync(message);
            }
        }

        /// <summary>
        /// Воспроизвести звук уведомления
        /// </summary>
        public async Task PlaySoundAsync(string? soundFile = null)
        {
            try
            {
                if (string.IsNullOrEmpty(soundFile))
                {
                    // Используем системный звук (только на Windows)
                    if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                    {
                        System.Console.Beep(800, 200); // Простой beep звук
                    }
                }
                else
                {
                    // TODO: Реализовать воспроизведение пользовательского звука
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NotificationService] Playing sound: {soundFile}"));
                }
            }
            catch (Exception ex)
            {
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NotificationService] Error playing sound: {ex.Message}"));
            }
        }

        /// <summary>
        /// Отправить сообщение в Discord
        /// </summary>
        public async Task SendDiscordMessageAsync(string message)
        {
            try
            {
                // TODO: Реализовать отправку в Discord через webhook
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[DISCORD] {message}"));
            }
            catch (Exception ex)
            {
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NotificationService] Error sending Discord message: {ex.Message}"));
            }
        }

        /// <summary>
        /// Отправить сообщение в Telegram
        /// </summary>
        public async Task SendTelegramMessageAsync(string message)
        {
            try
            {
                // TODO: Реализовать отправку в Telegram через Bot API
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[TELEGRAM] {message}"));
            }
            catch (Exception ex)
            {
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NotificationService] Error sending Telegram message: {ex.Message}"));
            }
        }

        /// <summary>
        /// Показать всплывающее уведомление
        /// </summary>
        public async Task ShowPopupNotificationAsync(ItemMatch match)
        {
            try
            {
                // TODO: Реализовать всплывающее уведомление в Windows
                var message = $"💰 {match.Item.ItemName} за {match.Item.FormattedPrice}";
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[POPUP] {message}"));
            }
            catch (Exception ex)
            {
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[NotificationService] Error showing popup: {ex.Message}"));
            }
        }
    }
}

using System;
using System.ComponentModel;

namespace L2Market.Domain.Models
{
    /// <summary>
    /// Правило отслеживания предметов
    /// </summary>
    public class TrackingRule : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = string.Empty;
        private MarketType _marketType;
        private int _itemId;
        private string _itemName = string.Empty;
        private long _maxPrice;
        private long _notificationPrice;
        private long _autoBuyPrice;
        private bool _isEnabled = true;
        private bool _hasNotifications = true;
        private bool _hasAutoBuy = false;
        private bool _playSound = true;
        private bool _sendDiscord = false;
        private bool _sendTelegram = false;
        private int _matchesFound;
        private DateTime _lastMatch;
        private DateTime _createdAt = DateTime.UtcNow;

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(nameof(Id)); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public MarketType MarketType
        {
            get => _marketType;
            set { _marketType = value; OnPropertyChanged(nameof(MarketType)); }
        }

        public int ItemId
        {
            get => _itemId;
            set { _itemId = value; OnPropertyChanged(nameof(ItemId)); }
        }

        public string ItemName
        {
            get => _itemName;
            set { _itemName = value; OnPropertyChanged(nameof(ItemName)); }
        }

        public long MaxPrice
        {
            get => _maxPrice;
            set { _maxPrice = value; OnPropertyChanged(nameof(MaxPrice)); }
        }

        public long NotificationPrice
        {
            get => _notificationPrice;
            set { _notificationPrice = value; OnPropertyChanged(nameof(NotificationPrice)); }
        }

        public long AutoBuyPrice
        {
            get => _autoBuyPrice;
            set { _autoBuyPrice = value; OnPropertyChanged(nameof(AutoBuyPrice)); }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(nameof(IsEnabled)); }
        }

        public bool HasNotifications
        {
            get => _hasNotifications;
            set { _hasNotifications = value; OnPropertyChanged(nameof(HasNotifications)); }
        }

        public bool HasAutoBuy
        {
            get => _hasAutoBuy;
            set { _hasAutoBuy = value; OnPropertyChanged(nameof(HasAutoBuy)); }
        }

        public bool PlaySound
        {
            get => _playSound;
            set { _playSound = value; OnPropertyChanged(nameof(PlaySound)); }
        }

        public bool SendDiscord
        {
            get => _sendDiscord;
            set { _sendDiscord = value; OnPropertyChanged(nameof(SendDiscord)); }
        }

        public bool SendTelegram
        {
            get => _sendTelegram;
            set { _sendTelegram = value; OnPropertyChanged(nameof(SendTelegram)); }
        }

        public int MatchesFound
        {
            get => _matchesFound;
            set { _matchesFound = value; OnPropertyChanged(nameof(MatchesFound)); }
        }

        public DateTime LastMatch
        {
            get => _lastMatch;
            set { _lastMatch = value; OnPropertyChanged(nameof(LastMatch)); }
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set { _createdAt = value; OnPropertyChanged(nameof(CreatedAt)); }
        }

        // Форматированные свойства
        public string FormattedMaxPrice => FormatPrice(_maxPrice);
        public string FormattedNotificationPrice => FormatPrice(_notificationPrice);
        public string FormattedAutoBuyPrice => FormatPrice(_autoBuyPrice);
        public string FormattedLastMatch => _lastMatch == DateTime.MinValue ? "Никогда" : _lastMatch.ToString("HH:mm:ss");

        private string FormatPrice(long price)
        {
            if (price >= 1_000_000_000)
                return $"{price / 1_000_000_000.0:F1}B";
            if (price >= 1_000_000)
                return $"{price / 1_000_000.0:F1}M";
            if (price >= 1_000)
                return $"{price / 1_000.0:F1}K";
            return price.ToString("N0");
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Типы рынков
    /// </summary>
    public enum MarketType
    {
        PrivateStore,
        Commission,
        WorldExchange,
        All
    }
}

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using L2Market.Domain.Models;
using L2Market.Core.Services;
using L2Market.Domain.Common;
using L2Market.Domain.Events;

namespace L2Market.UI.ViewModels
{
    /// <summary>
    /// ViewModel для окна рынка
    /// </summary>
    public class MarketWindowViewModel : INotifyPropertyChanged
    {
        private readonly TrackingService _trackingService;
        private readonly MarketManagerService _marketManager;
        private readonly NotificationService _notificationService;
        private readonly MarketQueryService _marketQueryService;
        private readonly ILocalEventBus _eventBus;
        private readonly Guid? _connectionId;

        private MarketType _selectedMarketType = MarketType.All;
        private string _selectedServer = "Cadmus";
        private string _searchText = string.Empty;
        private bool _isRefreshing;
        private int _totalItems;
        private int _activeRules;
        private string _lastUpdateTime = "Никогда";
        
        // Свойства для галочек отслеживания
        private bool _isPrivateStoreTrackingEnabled;
        private bool _isCommissionTrackingEnabled;
        private bool _isWorldExchangeTrackingEnabled;
        
        // Свойство для кнопки-тогла
        private bool _isTrackingActive;

        public MarketWindowViewModel(
            TrackingService trackingService,
            MarketManagerService marketManager,
            NotificationService notificationService,
            MarketQueryService marketQueryService,
            ILocalEventBus eventBus,
            Guid? connectionId = null)
        {
            _trackingService = trackingService ?? throw new ArgumentNullException(nameof(trackingService));
            _marketManager = marketManager ?? throw new ArgumentNullException(nameof(marketManager));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _marketQueryService = marketQueryService ?? throw new ArgumentNullException(nameof(marketQueryService));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _connectionId = connectionId;

            // Инициализация коллекций
            MarketItems = new ObservableCollection<MarketItemViewModel>();
            TrackingRules = new ObservableCollection<TrackingRule>();

            // Инициализация команд
            RefreshCommand = new RelayCommand(async () => await RefreshAsync());
            StartTrackingCommand = new RelayCommand(() => StartTracking());
            AddRuleCommand = new RelayCommand(() => AddRule());
            EditRuleCommand = new RelayCommand<TrackingRule>(rule => EditRule(rule));
            DeleteRuleCommand = new RelayCommand<TrackingRule>(async rule => await DeleteRuleAsync(rule));
            BuyItemCommand = new RelayCommand<MarketItemViewModel>(async item => await BuyItemAsync(item));
            ToggleTrackingCommand = new RelayCommand(() => ToggleTracking());

            // Подписка на события
            _trackingService.ItemMatchFound += OnItemMatchFound;
            
            // Подписка на события обновления рынка для автоматического обновления UI
            _eventBus.Subscribe<PrivateStoreUpdatedEvent>(OnPrivateStoreUpdated);
            _eventBus.Subscribe<CommissionUpdatedEvent>(OnCommissionUpdated);
            _eventBus.Subscribe<WorldExchangeUpdatedEvent>(OnWorldExchangeUpdated);

            // Загрузка данных
            _ = Task.Run(async () => await LoadDataAsync());
            
            // Автоматическая загрузка данных при создании ViewModel
            _ = Task.Run(async () => 
            {
                await Task.Delay(1000); // Небольшая задержка для инициализации
                await RefreshAsync();
            });
        }

        // Свойства
        public ObservableCollection<MarketItemViewModel> MarketItems { get; }
        public ObservableCollection<TrackingRule> TrackingRules { get; }
        
        // Команды
        public RelayCommand RefreshCommand { get; }
        public RelayCommand StartTrackingCommand { get; }
        public RelayCommand AddRuleCommand { get; }
        public RelayCommand<TrackingRule> EditRuleCommand { get; }
        public RelayCommand<TrackingRule> DeleteRuleCommand { get; }
        public RelayCommand<MarketItemViewModel> BuyItemCommand { get; }
        public RelayCommand ToggleTrackingCommand { get; }

        public MarketType SelectedMarketType
        {
            get => _selectedMarketType;
            set
            {
                _ = Task.Run(async () => await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MarketWindowViewModel] SelectedMarketType changed from {_selectedMarketType} to {value}")));
                _selectedMarketType = value;
                OnPropertyChanged(nameof(SelectedMarketType));
                _ = Task.Run(async () => await RefreshAsync());
            }
        }

        public string SelectedServer
        {
            get => _selectedServer;
            set
            {
                if (_selectedServer != value)
                {
                    _selectedServer = value;
                    OnPropertyChanged(nameof(SelectedServer));
                    // Здесь можно добавить логику для смены сервера
                    _ = Task.Run(async () => await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MarketWindowViewModel] SelectedServer changed to {value}")));
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                _ = Task.Run(async () => await FilterItemsAsync());
            }
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                _isRefreshing = value;
                OnPropertyChanged(nameof(IsRefreshing));
            }
        }

        public int TotalItems
        {
            get => _totalItems;
            set
            {
                _totalItems = value;
                OnPropertyChanged(nameof(TotalItems));
            }
        }

        public int ActiveRules
        {
            get => _activeRules;
            set
            {
                _activeRules = value;
                OnPropertyChanged(nameof(ActiveRules));
            }
        }


        public string LastUpdateTime
        {
            get => _lastUpdateTime;
            set
            {
                _lastUpdateTime = value;
                OnPropertyChanged(nameof(LastUpdateTime));
            }
        }

        // Свойства для галочек отслеживания
        public bool IsPrivateStoreTrackingEnabled
        {
            get => _isPrivateStoreTrackingEnabled;
            set
            {
                _isPrivateStoreTrackingEnabled = value;
                OnPropertyChanged(nameof(IsPrivateStoreTrackingEnabled));
                ToggleTracking(MarketType.PrivateStore, value);
            }
        }

        public bool IsCommissionTrackingEnabled
        {
            get => _isCommissionTrackingEnabled;
            set
            {
                _isCommissionTrackingEnabled = value;
                OnPropertyChanged(nameof(IsCommissionTrackingEnabled));
                ToggleTracking(MarketType.Commission, value);
            }
        }

        public bool IsWorldExchangeTrackingEnabled
        {
            get => _isWorldExchangeTrackingEnabled;
            set
            {
                _isWorldExchangeTrackingEnabled = value;
                OnPropertyChanged(nameof(IsWorldExchangeTrackingEnabled));
                ToggleTracking(MarketType.WorldExchange, value);
            }
        }

        public bool IsTrackingActive
        {
            get => _isTrackingActive;
            set
            {
                _isTrackingActive = value;
                OnPropertyChanged(nameof(IsTrackingActive));
                OnPropertyChanged(nameof(TrackingButtonText));
            }
        }

        public string TrackingButtonText => _isTrackingActive ? "⏹️ Остановить" : "▶️ Запустить";


        // Методы
        private async Task LoadDataAsync()
        {
            try
            {
                await RefreshAsync();
                await LoadTrackingRulesAsync();
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"Error loading data: {ex.Message}"));
            }
        }

        public async Task RefreshAsync()
        {
            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MarketWindowViewModel] RefreshAsync started"));
            IsRefreshing = true;
            try
            {
                // Получаем данные в зависимости от выбранного типа рынка
                var items = await GetMarketItemsAsync();
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MarketWindowViewModel] Got {items.Length} items from market"));
                
                // Проверяем, есть ли данные для отображения
                if (items.Length == 0)
                {
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MarketWindowViewModel] No items to display, skipping UI update"));
                    return;
                }
                
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MarketWindowViewModel] Updating UI with {items.Length} items"));
                
                // Обновляем UI в главном потоке
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    // Очищаем коллекцию перед добавлением новых предметов
                    MarketItems.Clear();
                    
                    // Добавляем новые предметы
                    foreach (var item in items)
                    {
                        MarketItems.Add(item);
                    }
                    TotalItems = MarketItems.Count;
                    LastUpdateTime = DateTime.Now.ToString("HH:mm:ss");
                    // System.Diagnostics.Debug.WriteLine($"[MarketWindowViewModel] UI updated with {MarketItems.Count} items");
                });
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private async Task<MarketItemViewModel[]> GetMarketItemsAsync()
        {
            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MarketWindowViewModel] GetMarketItemsAsync called with SelectedMarketType: {SelectedMarketType}"));
            
            var items = new List<MarketItemViewModel>();

            if (SelectedMarketType == MarketType.PrivateStore || SelectedMarketType == MarketType.All)
            {
                var privateStoreItems = await _marketManager.PrivateStores.GetAllItemsAsync();
                var privateStoreViewModels = privateStoreItems.Cast<object>().Select(item => 
                    MarketItemViewModel.FromPrivateStoreItem((L2Market.Domain.Entities.ExPrivateStoreSearchItemPacket.PrivateStoreItem)item));
                
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MarketWindowViewModel] PrivateStore items: {privateStoreViewModels.Count()}"));
                items.AddRange(privateStoreViewModels);
            }

            if (SelectedMarketType == MarketType.Commission || SelectedMarketType == MarketType.All)
            {
                var commissionItems = await _marketManager.Commissions.GetAllItemsAsync();
                var commissionViewModels = commissionItems.Cast<object>().Select(item => 
                    MarketItemViewModel.FromCommissionItem((L2Market.Domain.Entities.ExResponseCommissionListPacket.CommissionItem)item));
                
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MarketWindowViewModel] Commission items: {commissionViewModels.Count()}"));
                items.AddRange(commissionViewModels);
            }

            if (SelectedMarketType == MarketType.WorldExchange || SelectedMarketType == MarketType.All)
            {
                var worldExchangeItems = await _marketManager.WorldExchange.GetAllItemsAsync();
                var worldExchangeViewModels = worldExchangeItems.Cast<object>().Select(item => 
                    MarketItemViewModel.FromWorldExchangeItem((L2Market.Domain.Entities.WorldExchangeItemListPacket.WorldExchangeItemInfo)item));
                
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MarketWindowViewModel] WorldExchange items: {worldExchangeViewModels.Count()}"));
                items.AddRange(worldExchangeViewModels);
            }

            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MarketWindowViewModel] Total items returned: {items.Count}"));
            return items.ToArray();
        }

        private async Task LoadTrackingRulesAsync()
        {
            var rules = await _trackingService.GetRulesAsync();
            
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                TrackingRules.Clear();
                foreach (var rule in rules)
                {
                    TrackingRules.Add(rule);
                }
                ActiveRules = rules.Count(r => r.IsEnabled);
            });
        }

        private async Task FilterItemsAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await RefreshAsync();
                return;
            }

            var filteredItems = MarketItems.Where(item => 
                item.ItemName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                item.SellerName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                item.ItemId.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                MarketItems.Clear();
                foreach (var item in filteredItems)
                {
                    MarketItems.Add(item);
                }
                TotalItems = MarketItems.Count;
            });
        }

        private async void AddRule()
        {
            // TODO: Открыть окно добавления правила
            await _eventBus.PublishAsync(new LogMessageReceivedEvent("Add rule clicked"));
        }

        private void EditRule(TrackingRule? rule)
        {
            if (rule == null) return;
            // TODO: Открыть окно редактирования правила
            _ = Task.Run(async () => await _eventBus.PublishAsync(new LogMessageReceivedEvent($"Edit rule clicked: {rule.Name}")));
        }

        private async Task DeleteRuleAsync(TrackingRule? rule)
        {
            if (rule == null) return;
            
            await _trackingService.RemoveRuleAsync(rule.Id);
            await LoadTrackingRulesAsync();
        }

        private async Task BuyItemAsync(MarketItemViewModel? item)
        {
            if (item == null) return;
            
            // TODO: Реализовать покупку предмета
            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"Buy item clicked: {item.ItemName}"));
        }

        private async void OnItemMatchFound(object? sender, ItemMatchFoundEventArgs e)
        {
            await _notificationService.SendNotificationAsync(e.Match);
            await LoadTrackingRulesAsync(); // Обновляем счетчики
        }


        /// <summary>
        /// Включает/выключает отслеживание для типа рынка
        /// </summary>
        private async void ToggleTracking(MarketType marketType, bool isEnabled)
        {
            try
            {
                if (isEnabled && IsTrackingActive)
                {
                    // Включаем отслеживание с интервалом 30 секунд только если общее отслеживание активно
                    _marketQueryService.StartQuerying(marketType, TimeSpan.FromSeconds(30));
                }
                else
                {
                    // Выключаем отслеживание
                    _marketQueryService.StopQuerying(marketType);
                }
            }
            catch (Exception ex)
            {
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"Error toggling tracking for {marketType}: {ex.Message}"));
            }
        }

        /// <summary>
        /// Запускает отслеживание
        /// </summary>
        private void StartTracking()
        {
            try
            {
                IsTrackingActive = true;
                
                // Запускаем отслеживание для отмеченных типов рынка
                if (IsPrivateStoreTrackingEnabled)
                    _marketQueryService.StartQuerying(MarketType.PrivateStore, TimeSpan.FromSeconds(30));
                if (IsCommissionTrackingEnabled)
                    _marketQueryService.StartQuerying(MarketType.Commission, TimeSpan.FromSeconds(30));
                if (IsWorldExchangeTrackingEnabled)
                    _marketQueryService.StartQuerying(MarketType.WorldExchange, TimeSpan.FromSeconds(30));
                
                _ = Task.Run(async () => await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MarketWindowViewModel] Tracking started for server: {SelectedServer}")));
            }
            catch (Exception ex)
            {
                _ = Task.Run(async () => await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MarketWindowViewModel] Error starting tracking: {ex.Message}")));
            }
        }

        /// <summary>
        /// Переключает общее состояние отслеживания
        /// </summary>
        private void ToggleTracking()
        {
            try
            {
                IsTrackingActive = !IsTrackingActive;
                
                if (IsTrackingActive)
                {
                    // Запускаем отслеживание для отмеченных типов рынка
                    if (IsPrivateStoreTrackingEnabled)
                        _marketQueryService.StartQuerying(MarketType.PrivateStore, TimeSpan.FromSeconds(30));
                    if (IsCommissionTrackingEnabled)
                        _marketQueryService.StartQuerying(MarketType.Commission, TimeSpan.FromSeconds(30));
                    if (IsWorldExchangeTrackingEnabled)
                        _marketQueryService.StartQuerying(MarketType.WorldExchange, TimeSpan.FromSeconds(30));
                        
                    _eventBus.PublishAsync(new LogMessageReceivedEvent("🟢 Отслеживание запущено"));
                }
                else
                {
                    // Останавливаем все отслеживание
                    _marketQueryService.StopQuerying(MarketType.PrivateStore);
                    _marketQueryService.StopQuerying(MarketType.Commission);
                    _marketQueryService.StopQuerying(MarketType.WorldExchange);
                    
                    _eventBus.PublishAsync(new LogMessageReceivedEvent("🔴 Отслеживание остановлено"));
                }
            }
            catch (Exception ex)
            {
                _eventBus.PublishAsync(new LogMessageReceivedEvent($"Ошибка переключения отслеживания: {ex.Message}"));
            }
        }

        /// <summary>
        /// Обработка обновления частных магазинов
        /// </summary>
        private async Task OnPrivateStoreUpdated(PrivateStoreUpdatedEvent evt)
        {
            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MarketWindowViewModel] PrivateStore updated: {evt.Items.Count} items"));
            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MarketWindowViewModel] Triggering RefreshAsync..."));
            
            // Добавляем небольшую задержку, чтобы сервисы успели обработать данные
            await Task.Delay(100);
            await RefreshAsync(); // Автоматически обновляем UI
            
            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MarketWindowViewModel] RefreshAsync completed"));
        }

        /// <summary>
        /// Обработка обновления комиссий
        /// </summary>
        private async Task OnCommissionUpdated(CommissionUpdatedEvent evt)
        {
            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MarketWindowViewModel] Commission updated: {evt.Items.Count} items"));
            // Добавляем небольшую задержку, чтобы сервисы успели обработать данные
            await Task.Delay(100);
            await RefreshAsync(); // Автоматически обновляем UI
        }

        /// <summary>
        /// Обработка обновления мирового обмена
        /// </summary>
        private async Task OnWorldExchangeUpdated(WorldExchangeUpdatedEvent evt)
        {
            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[MarketWindowViewModel] WorldExchange updated: {evt.Items.Count} items"));
            // Добавляем небольшую задержку, чтобы сервисы успели обработать данные
            await Task.Delay(100);
            await RefreshAsync(); // Автоматически обновляем UI
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}

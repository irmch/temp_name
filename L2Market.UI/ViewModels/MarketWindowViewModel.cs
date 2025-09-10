using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using L2Market.Domain.Models;
using L2Market.Core.Services;
using L2Market.Domain.Common;

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
        private readonly AutoBuyService _autoBuyService;
        private readonly IEventBus _eventBus;

        private MarketType _selectedMarketType = MarketType.All;
        private string _searchText = string.Empty;
        private bool _isRefreshing;
        private int _totalItems;
        private int _activeRules;
        private string _lastUpdateTime = "Никогда";

        public MarketWindowViewModel(
            TrackingService trackingService,
            MarketManagerService marketManager,
            NotificationService notificationService,
            AutoBuyService autoBuyService,
            IEventBus eventBus)
        {
            _trackingService = trackingService ?? throw new ArgumentNullException(nameof(trackingService));
            _marketManager = marketManager ?? throw new ArgumentNullException(nameof(marketManager));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _autoBuyService = autoBuyService ?? throw new ArgumentNullException(nameof(autoBuyService));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            // Инициализация коллекций
            MarketItems = new ObservableCollection<MarketItemViewModel>();
            TrackingRules = new ObservableCollection<TrackingRule>();

            // Инициализация команд
            RefreshCommand = new RelayCommand(async () => await RefreshAsync());
            AddRuleCommand = new RelayCommand(() => AddRule());
            EditRuleCommand = new RelayCommand<TrackingRule>(rule => EditRule(rule));
            DeleteRuleCommand = new RelayCommand<TrackingRule>(async rule => await DeleteRuleAsync(rule));
            BuyItemCommand = new RelayCommand<MarketItemViewModel>(async item => await BuyItemAsync(item));

            // Подписка на события
            _trackingService.ItemMatchFound += OnItemMatchFound;
            _trackingService.AutoBuyTriggered += OnAutoBuyTriggered;
            
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

        public MarketType SelectedMarketType
        {
            get => _selectedMarketType;
            set
            {
                System.Diagnostics.Debug.WriteLine($"[MarketWindowViewModel] SelectedMarketType changed from {_selectedMarketType} to {value}");
                _selectedMarketType = value;
                OnPropertyChanged(nameof(SelectedMarketType));
                _ = Task.Run(async () => await RefreshAsync());
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

        public string AvailableMoney => _autoBuyService.GetFormattedMoney();

        public string LastUpdateTime
        {
            get => _lastUpdateTime;
            set
            {
                _lastUpdateTime = value;
                OnPropertyChanged(nameof(LastUpdateTime));
            }
        }

        // Команды
        public ICommand RefreshCommand { get; }
        public ICommand AddRuleCommand { get; }
        public ICommand EditRuleCommand { get; }
        public ICommand DeleteRuleCommand { get; }
        public ICommand BuyItemCommand { get; }

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
                System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
            }
        }

        public async Task RefreshAsync()
        {
            System.Diagnostics.Debug.WriteLine($"[MarketWindowViewModel] RefreshAsync started");
            IsRefreshing = true;
            try
            {
                // Получаем данные в зависимости от выбранного типа рынка
                var items = await GetMarketItemsAsync();
                System.Diagnostics.Debug.WriteLine($"[MarketWindowViewModel] Got {items.Length} items from market");
                
                // Проверяем, есть ли данные для отображения
                if (items.Length == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[MarketWindowViewModel] No items to display, skipping UI update");
                    return;
                }
                
                // Обновляем UI в главном потоке
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var item in items)
                    {
                        MarketItems.Add(item);
                    }
                    TotalItems = MarketItems.Count;
                    LastUpdateTime = DateTime.Now.ToString("HH:mm:ss");
                    System.Diagnostics.Debug.WriteLine($"[MarketWindowViewModel] UI updated with {MarketItems.Count} items");
                });
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private async Task<MarketItemViewModel[]> GetMarketItemsAsync()
        {
            System.Diagnostics.Debug.WriteLine($"[MarketWindowViewModel] GetMarketItemsAsync called with SelectedMarketType: {SelectedMarketType}");
            
            // Очищаем коллекцию в начале
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                MarketItems.Clear();
            });
            
            var items = new List<MarketItemViewModel>();

            if (SelectedMarketType == MarketType.PrivateStore || SelectedMarketType == MarketType.All)
            {
                var privateStoreItems = await _marketManager.PrivateStores.GetAllItemsAsync();
                var privateStoreViewModels = privateStoreItems.Cast<object>().Select(item => 
                    MarketItemViewModel.FromPrivateStoreItem((L2Market.Domain.Entities.ExPrivateStoreSearchItemPacket.PrivateStoreItem)item));
                
                System.Diagnostics.Debug.WriteLine($"[MarketWindowViewModel] PrivateStore items: {privateStoreViewModels.Count()}");
                items.AddRange(privateStoreViewModels);
            }

            if (SelectedMarketType == MarketType.Commission || SelectedMarketType == MarketType.All)
            {
                var commissionItems = await _marketManager.Commissions.GetAllItemsAsync();
                var commissionViewModels = commissionItems.Cast<object>().Select(item => 
                    MarketItemViewModel.FromCommissionItem((L2Market.Domain.Entities.ExResponseCommissionListPacket.CommissionItem)item));
                
                System.Diagnostics.Debug.WriteLine($"[MarketWindowViewModel] Commission items: {commissionViewModels.Count()}");
                items.AddRange(commissionViewModels);
            }

            if (SelectedMarketType == MarketType.WorldExchange || SelectedMarketType == MarketType.All)
            {
                var worldExchangeItems = await _marketManager.WorldExchange.GetAllItemsAsync();
                var worldExchangeViewModels = worldExchangeItems.Cast<object>().Select(item => 
                    MarketItemViewModel.FromWorldExchangeItem((L2Market.Domain.Entities.WorldExchangeItemListPacket.WorldExchangeItemInfo)item));
                
                System.Diagnostics.Debug.WriteLine($"[MarketWindowViewModel] WorldExchange items: {worldExchangeViewModels.Count()}");
                items.AddRange(worldExchangeViewModels);
            }

            System.Diagnostics.Debug.WriteLine($"[MarketWindowViewModel] Total items returned: {items.Count}");
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

        private void AddRule()
        {
            // TODO: Открыть окно добавления правила
            System.Diagnostics.Debug.WriteLine("Add rule clicked");
        }

        private void EditRule(TrackingRule? rule)
        {
            if (rule == null) return;
            // TODO: Открыть окно редактирования правила
            System.Diagnostics.Debug.WriteLine($"Edit rule clicked: {rule.Name}");
        }

        private async Task DeleteRuleAsync(TrackingRule? rule)
        {
            if (rule == null) return;
            
            await _trackingService.RemoveRuleAsync(rule.Id);
            await LoadTrackingRulesAsync();
        }

        private Task BuyItemAsync(MarketItemViewModel? item)
        {
            if (item == null) return Task.CompletedTask;
            
            // TODO: Реализовать покупку предмета
            System.Diagnostics.Debug.WriteLine($"Buy item clicked: {item.ItemName}");
            return Task.CompletedTask;
        }

        private async void OnItemMatchFound(object? sender, ItemMatchFoundEventArgs e)
        {
            await _notificationService.SendNotificationAsync(e.Match);
            await LoadTrackingRulesAsync(); // Обновляем счетчики
        }

        private async void OnAutoBuyTriggered(object? sender, AutoBuyTriggeredEventArgs e)
        {
            await _autoBuyService.TryAutoBuyAsync(e.Match);
            await LoadTrackingRulesAsync(); // Обновляем счетчики
        }

        /// <summary>
        /// Обработка обновления частных магазинов
        /// </summary>
        private async Task OnPrivateStoreUpdated(PrivateStoreUpdatedEvent evt)
        {
            System.Diagnostics.Debug.WriteLine($"[MarketWindowViewModel] PrivateStore updated: {evt.Items.Count} items");
            // Добавляем небольшую задержку, чтобы сервисы успели обработать данные
            await Task.Delay(100);
            await RefreshAsync(); // Автоматически обновляем UI
        }

        /// <summary>
        /// Обработка обновления комиссий
        /// </summary>
        private async Task OnCommissionUpdated(CommissionUpdatedEvent evt)
        {
            System.Diagnostics.Debug.WriteLine($"[MarketWindowViewModel] Commission updated: {evt.Items.Count} items");
            // Добавляем небольшую задержку, чтобы сервисы успели обработать данные
            await Task.Delay(100);
            await RefreshAsync(); // Автоматически обновляем UI
        }

        /// <summary>
        /// Обработка обновления мирового обмена
        /// </summary>
        private async Task OnWorldExchangeUpdated(WorldExchangeUpdatedEvent evt)
        {
            System.Diagnostics.Debug.WriteLine($"[MarketWindowViewModel] WorldExchange updated: {evt.Items.Count} items");
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

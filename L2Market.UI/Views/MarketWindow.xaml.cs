using System;
using System.Windows;
using L2Market.UI.ViewModels;
using L2Market.Domain.Models;

namespace L2Market.UI.Views
{
    /// <summary>
    /// Окно рынка
    /// </summary>
    public partial class MarketWindow : Window
    {
        public MarketWindowViewModel ViewModel { get; }

        public MarketWindow(MarketWindowViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = ViewModel;

            // Настройка ComboBox
            MarketTypeComboBox.SelectionChanged += (s, e) =>
            {
                try
                {
                    if (MarketTypeComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem item)
                    {
                        var tag = item.Tag?.ToString();
                        if (Enum.TryParse<MarketType>(tag, out var marketType))
                        {
                            ViewModel.SelectedMarketType = marketType;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in ComboBox selection: {ex.Message}");
                }
            };

            // Устанавливаем "Все" по умолчанию
            MarketTypeComboBox.SelectedIndex = 0;
            ViewModel.SelectedMarketType = MarketType.All;
            
            // Автоматически загружаем данные при открытии окна
            _ = Task.Run(async () => 
            {
                await Task.Delay(500); // Небольшая задержка
                await ViewModel.RefreshAsync();
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            // Скрываем окно вместо закрытия
            Hide();
            base.OnClosed(e);
        }
    }
}

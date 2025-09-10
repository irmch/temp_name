using L2Market.Core.Configuration;
using L2Market.UI.ViewModels;
using System.Windows;

namespace L2Market.UI.Views
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow(IConfigurationService configurationService)
        {
            InitializeComponent();
            DataContext = new SettingsViewModel(configurationService);
        }
    }
}

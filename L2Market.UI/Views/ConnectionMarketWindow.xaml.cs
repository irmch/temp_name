using System.Windows;
using L2Market.Core.Services;
using L2Market.Domain.Models;
using L2Market.UI.ViewModels;

namespace L2Market.UI.Views
{
    /// <summary>
    /// Interaction logic for ConnectionMarketWindow.xaml
    /// </summary>
    public partial class ConnectionMarketWindow : Window
    {
        public ConnectionMarketWindow(ConnectionInfo connectionInfo, ConnectionScope connectionScope)
        {
            InitializeComponent();
            DataContext = new ConnectionMarketViewModel(connectionInfo, connectionScope);
        }
    }
}

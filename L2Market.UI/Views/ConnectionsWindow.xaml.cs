using System.Windows;
using L2Market.UI.ViewModels;

namespace L2Market.UI.Views
{
    /// <summary>
    /// Interaction logic for ConnectionsWindow.xaml
    /// </summary>
    public partial class ConnectionsWindow : Window
    {
        public ConnectionsWindow()
        {
            InitializeComponent();
        }
        
        public ConnectionsWindow(ConnectionsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}

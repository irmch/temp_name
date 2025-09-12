using System.Windows;
using L2Market.UI.ViewModels;

namespace L2Market.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel? _viewModel;

        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(MainViewModel viewModel) : this()
        {
            _viewModel = viewModel;
            DataContext = _viewModel;
            
            // Add window closing handler
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                _viewModel?.LogMessage("Closing application...");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during application shutdown: {ex.Message}");
            }
        }

        private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (_viewModel?.SendChatMessageCommand?.CanExecute(null) == true)
                {
                    _viewModel.SendChatMessageCommand.Execute(null);
                }
            }
        }

    }
}
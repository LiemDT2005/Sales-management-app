using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;
using SMA.ViewModels;

namespace SMA.Views.Admin
{
    /// <summary>
    /// Interaction logic for StockWarningWindow.xaml
    /// </summary>
    public partial class StockWarningWindow : Window
    {
        public StockWarningWindow(StockWarningViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        // Chỉ cho phép nhập số
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}

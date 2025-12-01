using System.Windows;
using SMA.ViewModels.AuthViewModels;


namespace SMA.Views.Auth
{
    /// <summary>
    /// Interaction logic for ChangePasswordView.xaml
    /// </summary>
    public partial class ChangePasswordView : Window
    {
        public ChangePasswordView()
        {
            InitializeComponent();
            DataContext = new ChangePasswordViewModel();
        }
    }
}

using System.Windows;
using SMA.ViewModels.AuthViewModels;

namespace SMA.Views.Auth
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
            DataContext = new LoginViewModel();
        }
    }
}

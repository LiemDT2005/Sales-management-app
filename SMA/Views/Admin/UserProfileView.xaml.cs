using System.Windows;
using SMA.Models;
using SMA.ViewModels.UserViewModels;

namespace SMA.Views.Admin
{
    /// <summary>
    /// Interaction logic for UserProfileView.xaml
    /// </summary>
    public partial class UserProfileView : Window
    {
        public UserProfileView(User user)
        {
            InitializeComponent();
            DataContext = new UserProfileViewModel(user);
        }
    }
}

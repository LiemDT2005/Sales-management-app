using System.Windows.Controls;
using SMA.ViewModels.UserViewModels;

namespace SMA.Views.Admin
{
    /// <summary>
    /// Interaction logic for UserManagementView.xaml
    /// </summary>
    public partial class UserManagementView : UserControl
    {
        public UserManagementView()
        {
            InitializeComponent();
            DataContext = new UserManagementViewModel();
        }
    }
}

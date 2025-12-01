using System.Windows;
using SMA.ViewModels.UserViewModels;

namespace SMA.Views.Admin
{
    /// <summary>
    /// Interaction logic for AddUserView.xaml
    /// </summary>
    public partial class AddUserView : Window
    {
        public AddUserView()
        {
            InitializeComponent();
            DataContext = new AddUserViewModel();
        }
    }
}

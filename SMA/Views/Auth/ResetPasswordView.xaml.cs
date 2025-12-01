using System.Windows;
using SMA.ViewModels;

namespace SMA.Views.Auth
{
    /// <summary>
    /// Interaction logic for ResetPasswordView.xaml
    /// </summary>
    public partial class ResetPasswordView : Window
    {
        public ResetPasswordView()
        {
            InitializeComponent();
        }

        // ✅ REMOVED NewPasswordBox_PasswordChanged event handler
        // ✅ REMOVED ConfirmPasswordBox_PasswordChanged event handler
        // Now using PasswordBoxHelper (Attached Property) for MVVM-compliant binding
        // See: SMA/Helper/PasswordBoxHelper.cs
    }
}

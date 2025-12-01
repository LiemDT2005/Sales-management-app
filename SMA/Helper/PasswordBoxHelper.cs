using System.Windows;
using System.Windows.Controls;

namespace SMA.Helper
{
    /// <summary>
    /// Attached Property helper for PasswordBox to enable MVVM-compliant two-way binding.
    /// This is infrastructure code, not business logic, so it's MVVM compliant.
    /// 
    /// Usage in XAML:
    /// <PasswordBox helper:PasswordBoxHelper.BindPassword="True"
    ///              helper:PasswordBoxHelper.BoundPassword="{Binding Password, Mode=TwoWay}"/>
    /// </summary>
    public static class PasswordBoxHelper
    {
        // ===================================================================
        // Attached Property: BoundPassword
        // Binds to ViewModel property (two-way binding)
        // ===================================================================
        public static readonly DependencyProperty BoundPasswordProperty =
            DependencyProperty.RegisterAttached(
                "BoundPassword",
                typeof(string),
                typeof(PasswordBoxHelper),
                new PropertyMetadata(string.Empty, OnBoundPasswordChanged));

        public static string GetBoundPassword(DependencyObject d)
        {
            return (string)d.GetValue(BoundPasswordProperty);
        }

        public static void SetBoundPassword(DependencyObject d, string value)
        {
            d.SetValue(BoundPasswordProperty, value);
        }

        // ===================================================================
        // Attached Property: BindPassword
        // Set to True to enable binding
        // ===================================================================
        public static readonly DependencyProperty BindPasswordProperty =
            DependencyProperty.RegisterAttached(
                "BindPassword",
                typeof(bool),
                typeof(PasswordBoxHelper),
                new PropertyMetadata(false, OnBindPasswordChanged));

        public static bool GetBindPassword(DependencyObject d)
        {
            return (bool)d.GetValue(BindPasswordProperty);
        }

        public static void SetBindPassword(DependencyObject d, bool value)
        {
            d.SetValue(BindPasswordProperty, value);
        }

        // ===================================================================
        // Internal Property: UpdatingPassword
        // Prevents infinite loop during updates
        // ===================================================================
        private static readonly DependencyProperty UpdatingPasswordProperty =
            DependencyProperty.RegisterAttached(
                "UpdatingPassword",
                typeof(bool),
                typeof(PasswordBoxHelper));

        private static bool GetUpdatingPassword(DependencyObject d)
        {
            return (bool)d.GetValue(UpdatingPasswordProperty);
        }

        private static void SetUpdatingPassword(DependencyObject d, bool value)
        {
            d.SetValue(UpdatingPasswordProperty, value);
        }

        // ===================================================================
        // Event Handlers
        // ===================================================================

        /// <summary>
        /// Called when BoundPassword property changes in ViewModel
        /// Updates PasswordBox.Password
        /// </summary>
        private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBox passwordBox)
            {
                // Prevent infinite loop
                if (GetUpdatingPassword(passwordBox))
                    return;

                // Detach event handler to prevent triggering during update
                passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;

                // Update PasswordBox
                if (e.NewValue != null)
                {
                    passwordBox.Password = e.NewValue.ToString() ?? string.Empty;
                }
                else
                {
                    passwordBox.Password = string.Empty;
                }

                // Reattach event handler
                passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
            }
        }

        /// <summary>
        /// Called when BindPassword is set to True
        /// Attaches/detaches event handler
        /// </summary>
        private static void OnBindPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBox passwordBox)
            {
                bool bindPassword = (bool)e.NewValue;

                if (bindPassword)
                {
                    // Attach event handler
                    passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
                }
                else
                {
                    // Detach event handler
                    passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;
                }
            }
        }

        /// <summary>
        /// Called when PasswordBox.Password changes
        /// Updates BoundPassword property (which updates ViewModel)
        /// </summary>
        private static void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                // Set flag to prevent infinite loop
                SetUpdatingPassword(passwordBox, true);

                // Update BoundPassword (which updates ViewModel)
                SetBoundPassword(passwordBox, passwordBox.Password);

                // Clear flag
                SetUpdatingPassword(passwordBox, false);
            }
        }
    }
}

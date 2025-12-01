using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using SMA.Helper;
using SMA.Services;

namespace SMA.ViewModels.AuthViewModels
{
    public class ResetPasswordViewModel : INotifyPropertyChanged
    {
        private readonly AuthService _authService;
        private readonly string _email;
        private string _newPassword;
        private string _confirmPassword;
        private bool _isLoading;
        private string _errorMessage;
        private string _successMessage;
        private bool _isNewPasswordVisible;
        private bool _isConfirmPasswordVisible;

        // Properties
        public string Email => _email;

        public string NewPassword
        {
            get => _newPassword;
            set
            {
                _newPassword = value;
                OnPropertyChanged();
                ClearMessages();
            }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                _confirmPassword = value;
                OnPropertyChanged();
                ClearMessages();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        public string SuccessMessage
        {
            get => _successMessage;
            set
            {
                _successMessage = value;
                OnPropertyChanged();
            }
        }

        public bool IsNewPasswordVisible
        {
            get => _isNewPasswordVisible;
            set
            {
                _isNewPasswordVisible = value;
                OnPropertyChanged();
            }
        }

        public bool IsConfirmPasswordVisible
        {
            get => _isConfirmPasswordVisible;
            set
            {
                _isConfirmPasswordVisible = value;
                OnPropertyChanged();
            }
        }

        // Commands
        public ICommand ResetPasswordCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ToggleNewPasswordVisibilityCommand { get; }
        public ICommand ToggleConfirmPasswordVisibilityCommand { get; }

        public ResetPasswordViewModel(string email)
        {
            _authService = new AuthService();
            _email = email;
            _newPassword = string.Empty;
            _confirmPassword = string.Empty;
            _isLoading = false;
            _errorMessage = string.Empty;
            _successMessage = string.Empty;
            _isNewPasswordVisible = false;
            _isConfirmPasswordVisible = false;

            ResetPasswordCommand = new RelayCommand(ExecuteResetPassword, CanExecuteResetPassword);
            CancelCommand = new RelayCommand(ExecuteCancel);
            ToggleNewPasswordVisibilityCommand = new RelayCommand(_ => ToggleNewPasswordVisibility());
            ToggleConfirmPasswordVisibilityCommand = new RelayCommand(_ => ToggleConfirmPasswordVisibility());
        }

        private bool CanExecuteResetPassword(object parameter)
        {
            return !IsLoading &&
                   !string.IsNullOrWhiteSpace(NewPassword) &&
                   !string.IsNullOrWhiteSpace(ConfirmPassword);
        }

        private void ExecuteResetPassword(object parameter)
        {
            ClearMessages();

            // Validate passwords
            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                ErrorMessage = "Please enter a new password.";
                return;
            }

            if (NewPassword.Length < 6)
            {
                ErrorMessage = "Password must be at least 6 characters long.";
                return;
            }

            if (NewPassword != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match. Please try again.";
                return;
            }

            IsLoading = true;

            try
            {
                // Update password in database
                bool success = _authService.UpdatePassword(_email, NewPassword);

                if (success)
                {
                    SuccessMessage = "Password reset successfully! Redirecting to login...";

                    // Clear OTP completely after successful password reset
                    _authService.ClearOtp(_email); // ← Xóa hoàn toàn OTP và expiry

                    // Wait a moment then redirect to login
                    Task.Delay(2000).ContinueWith(_ =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            // Open Login window
                            var loginWindow = new Views.Auth.Login();
                            loginWindow.Show();

                            // Close current window
                            CloseWindow(parameter);
                        });
                    });
                }
                else
                {
                    ErrorMessage = "Failed to reset password. Please try again.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"An error occurred: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteCancel(object parameter)
        {
            // Invalidate OTP when canceling (keep for audit trail)
            try
            {
                _authService.InvalidateOtp(_email); // ← Set expiry to past
            }
            catch { }

            CloseWindow(parameter);
        }

        private void CloseWindow(object parameter)
        {
            if (parameter is Window win)
            {
                win.Close();
            }
            else
            {
                // Try to find the window from Application.Current.Windows
                foreach (Window w in Application.Current.Windows)
                {
                    if (w.DataContext == this)
                    {
                        w.Close();
                        break;
                    }
                }
            }
        }

        private void ClearMessages()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
        }

        private void ToggleNewPasswordVisibility()
        {
            IsNewPasswordVisible = !IsNewPasswordVisible;
        }

        private void ToggleConfirmPasswordVisibility()
        {
            IsConfirmPasswordVisible = !IsConfirmPasswordVisible;
        }

        // INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using SMA.Helper;
using SMA.Services;

namespace SMA.ViewModels.AuthViewModels
{
    public class ChangePasswordViewModel : INotifyPropertyChanged
    {
        private readonly AuthService _authService;
        private string _currentPassword;
        private string _newPassword;
        private string _confirmPassword;
        private string _errorMessage;
        private string _successMessage;
        private bool _isLoading;
        private bool _isCurrentPasswordVisible;
        private bool _isNewPasswordVisible;
        private bool _isConfirmPasswordVisible;

        // Properties
        public string CurrentPassword
        {
            get => _currentPassword;
            set
            {
                _currentPassword = value;
                OnPropertyChanged();
                ClearMessages();
            }
        }

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

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public bool IsCurrentPasswordVisible
        {
            get => _isCurrentPasswordVisible;
            set
            {
                _isCurrentPasswordVisible = value;
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
        public ICommand ChangePasswordCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ToggleCurrentPasswordVisibilityCommand { get; }
        public ICommand ToggleNewPasswordVisibilityCommand { get; }
        public ICommand ToggleConfirmPasswordVisibilityCommand { get; }

        public ChangePasswordViewModel()
        {
            _authService = new AuthService();
            _currentPassword = string.Empty;
            _newPassword = string.Empty;
            _confirmPassword = string.Empty;
            _errorMessage = string.Empty;
            _successMessage = string.Empty;
            _isLoading = false;
            _isCurrentPasswordVisible = false;
            _isNewPasswordVisible = false;
            _isConfirmPasswordVisible = false;

            ChangePasswordCommand = new RelayCommand(ExecuteChangePassword, CanExecuteChangePassword);
            CancelCommand = new RelayCommand(ExecuteCancel);
            ToggleCurrentPasswordVisibilityCommand = new RelayCommand(_ => ToggleCurrentPasswordVisibility());
            ToggleNewPasswordVisibilityCommand = new RelayCommand(_ => ToggleNewPasswordVisibility());
            ToggleConfirmPasswordVisibilityCommand = new RelayCommand(_ => ToggleConfirmPasswordVisibility());
        }

        private void ToggleCurrentPasswordVisibility()
        {
            IsCurrentPasswordVisible = !IsCurrentPasswordVisible;
        }

        private void ToggleNewPasswordVisibility()
        {
            IsNewPasswordVisible = !IsNewPasswordVisible;
        }

        private void ToggleConfirmPasswordVisibility()
        {
            IsConfirmPasswordVisible = !IsConfirmPasswordVisible;
        }

        private bool CanExecuteChangePassword(object parameter)
        {
            return !IsLoading && 
                   !string.IsNullOrWhiteSpace(CurrentPassword) &&
                   !string.IsNullOrWhiteSpace(NewPassword) && 
                   !string.IsNullOrWhiteSpace(ConfirmPassword);
        }

        private void ExecuteChangePassword(object parameter)
        {
            ClearMessages();

            // Check if user is logged in
            if (!SessionManager.IsLoggedIn)
            {
                ErrorMessage = "No user is currently logged in.";
                return;
            }

            // Validate current password
            if (string.IsNullOrWhiteSpace(CurrentPassword))
            {
                ErrorMessage = "Please enter your current password.";
                return;
            }

            // Verify current password
            var user = _authService.Authenticate(SessionManager.CurrentUserEmail, CurrentPassword);
            if (user == null)
            {
                ErrorMessage = "Current password is incorrect.";
                return;
            }

            // Validate new password
            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                ErrorMessage = "Please enter a new password.";
                return;
            }

            if (NewPassword.Length < 6)
            {
                ErrorMessage = "New password must be at least 6 characters long.";
                return;
            }

            if (NewPassword == CurrentPassword)
            {
                ErrorMessage = "New password must be different from current password.";
                return;
            }

            if (string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                ErrorMessage = "Please confirm your new password.";
                return;
            }

            if (NewPassword != ConfirmPassword)
            {
                ErrorMessage = "New passwords do not match.";
                return;
            }

            IsLoading = true;

            try
            {
                // Update password in database
                bool success = _authService.UpdatePassword(SessionManager.CurrentUserEmail, NewPassword);

                if (success)
                {
                    SuccessMessage = "Password has been changed successfully!";

                    // Show success message
                    MessageBox.Show("Your password has been changed successfully.",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Close window
                    CloseWindow(parameter);
                }
                else
                {
                    ErrorMessage = "Failed to change password. Please try again.";
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

        // INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

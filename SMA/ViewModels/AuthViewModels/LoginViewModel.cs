using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using SMA.Helper;
using SMA.Services;
using SMA.Models;

namespace SMA.ViewModels.AuthViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly AuthService _authService;
        private string _email;
        private string _password;
        private string _errorMessage;
        private bool _isLoading;
        private bool _isPasswordVisible;

        // Properties
        public string Email
        {
            get => _email;
            set
            {
                _email = value;
                OnPropertyChanged();
                ClearErrorMessage();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
                ClearErrorMessage();
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

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public bool IsPasswordVisible
        {
            get => _isPasswordVisible;
            set
            {
                _isPasswordVisible = value;
                OnPropertyChanged();
            }
        }

        // Commands
        public ICommand LoginCommand { get; }
        public ICommand ForgotPasswordCommand { get; }
        public ICommand TogglePasswordVisibilityCommand { get; }

        public LoginViewModel()
        {
            _authService = new AuthService();

            // Initialize Commands
            LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
            ForgotPasswordCommand = new RelayCommand(ExecuteForgotPassword);
            TogglePasswordVisibilityCommand = new RelayCommand(_ => TogglePasswordVisibility());

            // Initialize properties
            _email = string.Empty;
            _password = string.Empty;
            _errorMessage = string.Empty;
            _isLoading = false;
            _isPasswordVisible = false;
        }

        private bool CanExecuteLogin(object parameter)
        {
            return !IsLoading && !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);
        }

        private void ExecuteLogin(object parameter)
        {
            // Clear previous error
            ErrorMessage = string.Empty;

            // Validate Email
            if (string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = "Please enter your email.";
                return;
            }

            // Validate Email format
            if (!IsValidEmail(Email))
            {
                ErrorMessage = "Please enter a valid email address.";
                return;
            }

            // Validate Password
            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter your password.";
                return;
            }

            // Show loading
            IsLoading = true;

            try
            {
                // Authenticate user
                User? user = _authService.Authenticate(Email, Password);

                if (user != null)
                {
                    // Check user status
                    if (user.Status?.ToLower() == "banned")
                    {
                        ErrorMessage = "Your account has been banned. Please contact administrator for more information.";
                        IsLoading = false;
                        return;
                    }

                    if (user.Status?.ToLower() == "inactive")
                    {
                        ErrorMessage = "Your account has been deactivated. Please contact administrator.";
                        IsLoading = false;
                        return;
                    }

                    // Only allow Active users to login
                    if (user.Status?.ToLower() != "active")
                    {
                        ErrorMessage = "Your account status is invalid. Please contact administrator.";
                        IsLoading = false;
                        return;
                    }

                    // Authentication successful - store in session
                    SessionManager.Login(user);

                    // Open MainWindow
                    var mainWindow = new MainWindow();
                    mainWindow.Show();

                    // Close login window
                    CloseLoginWindow(parameter);
                }
                else
                {
                    ErrorMessage = "Invalid email or password.";
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

        private void ExecuteForgotPassword(object parameter)
        {
            // Open Forgot Password window
            var forgotPasswordView = new Views.Auth.ForgotPasswordView();
            forgotPasswordView.ShowDialog();
        }

        private void TogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        private void CloseLoginWindow(object parameter)
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

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void ClearErrorMessage()
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ErrorMessage = string.Empty;
            }
        }

        // INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

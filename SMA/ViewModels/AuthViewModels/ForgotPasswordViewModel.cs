using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using SMA.Helper;
using SMA.Services;

namespace SMA.ViewModels.AuthViewModels
{
    public class ForgotPasswordViewModel : INotifyPropertyChanged
    {
        private readonly EmailService _emailService;
        private readonly AuthService _authService;
        private string _email;
        private string _otp;
        private bool _isOtpSent;
        private bool _isLoading;
        private string _errorMessage;
        private string _successMessage;
        private string _sendOtpButtonText;
        private int _remainingAttempts;

        // Properties
        public string Email
        {
            get => _email;
            set
            {
                _email = value;
                OnPropertyChanged();
                ClearMessages();
            }
        }

        public string Otp
        {
            get => _otp;
            set
            {
                _otp = value;
                OnPropertyChanged();
                ClearMessages();
            }
        }

        public bool IsOtpSent
        {
            get => _isOtpSent;
            set
            {
                _isOtpSent = value;
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

        public string SendOtpButtonText
        {
            get => _sendOtpButtonText;
            set
            {
                _sendOtpButtonText = value;
                OnPropertyChanged();
            }
        }

        public int RemainingAttempts
        {
            get => _remainingAttempts;
            set
            {
                _remainingAttempts = value;
                OnPropertyChanged();
            }
        }

        // Commands
        public ICommand SendOtpCommand { get; }
        public ICommand VerifyOtpCommand { get; }
        public ICommand BackToLoginCommand { get; }

        public ForgotPasswordViewModel()
        {
            _emailService = new EmailService();
            _authService = new AuthService();
            _email = string.Empty;
            _otp = string.Empty;
            _isOtpSent = false;
            _isLoading = false;
            _errorMessage = string.Empty;
            _successMessage = string.Empty;
            _sendOtpButtonText = "Send OTP";
            _remainingAttempts = 5;

            SendOtpCommand = new RelayCommand(ExecuteSendOtp, CanExecuteSendOtp);
            VerifyOtpCommand = new RelayCommand(ExecuteVerifyOtp, CanExecuteVerifyOtp);
            BackToLoginCommand = new RelayCommand(ExecuteBackToLogin);
        }

        private bool CanExecuteSendOtp(object parameter)
        {
            return !IsLoading && !string.IsNullOrWhiteSpace(Email) && !IsOtpSent;
        }

        private async void ExecuteSendOtp(object parameter)
        {
            ClearMessages();

            // Validate Email
            if (string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = "Please enter your email address.";
                return;
            }

            if (!IsValidEmail(Email))
            {
                ErrorMessage = "Please enter a valid email address.";
                return;
            }

            IsLoading = true;
            SendOtpButtonText = "Sending...";

            try
            {
                // Check if email exists in database
                var user = _authService.GetUserByEmail(Email);
                if (user == null)
                {
                    ErrorMessage = "Email not found in our system.";
                    SendOtpButtonText = "Send OTP";
                    return;
                }

                // Generate OTP and save to database
                string generatedOtp = _authService.GenerateAndSaveOtp(Email);

                // Send OTP via email
                bool emailSent = await _emailService.SendOtpEmailAsync(Email, generatedOtp);

                if (emailSent)
                {
                    IsOtpSent = true;
                    RemainingAttempts = 5;
                    SuccessMessage = $"A 6-digit OTP has been sent to {Email}. Please check your inbox and spam folder. The OTP is valid for 5 minutes.";
                    SendOtpButtonText = "OTP Sent";
                }
                else
                {
                    ErrorMessage = "Failed to send OTP email. Please check your email address and try again.";
                    SendOtpButtonText = "Send OTP";

                    // Invalidate OTP if email failed (set expiry to past)
                    _authService.InvalidateOtp(Email);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"An error occurred: {ex.Message}";
                SendOtpButtonText = "Send OTP";

                // Invalidate OTP on error
                try
                {
                    _authService.InvalidateOtp(Email);
                }
                catch { }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanExecuteVerifyOtp(object parameter)
        {
            return !IsLoading && !string.IsNullOrWhiteSpace(Otp) && IsOtpSent;
        }

        private void ExecuteVerifyOtp(object parameter)
        {
            ClearMessages();

            // Validate OTP input
            if (string.IsNullOrWhiteSpace(Otp))
            {
                ErrorMessage = "Please enter the OTP code.";
                return;
            }

            if (Otp.Length != 6)
            {
                ErrorMessage = "OTP must be 6 digits.";
                return;
            }

            // Check remaining attempts
            if (_remainingAttempts <= 0)
            {
                ErrorMessage = "You have exceeded the maximum number of attempts. Please request a new OTP.";
                ResetOtpState();
                return;
            }

            IsLoading = true;

            try
            {
                // Verify OTP from database
                bool isValid = _authService.VerifyOtp(Email, Otp.Trim());

                if (isValid)
                {
                    // OTP is correct
                    SuccessMessage = "OTP verified successfully! Redirecting to reset password...";

                    // Don't clear or invalidate OTP yet - it will be cleared after password reset

                    // Navigate to Reset Password View
                    var resetPasswordView = new Views.Auth.ResetPasswordView();
                    var resetPasswordViewModel = new ResetPasswordViewModel(Email);
                    resetPasswordView.DataContext = resetPasswordViewModel;
                    resetPasswordView.Show();

                    // Close current window
                    CloseWindow(parameter);
                }
                else
                {
                    // OTP is incorrect or expired
                    _remainingAttempts--;

                    // Check if OTP expired
                    if (_authService.IsOtpExpired(Email))
                    {
                        ErrorMessage = "OTP has expired. Please request a new one.";
                        ResetOtpState();
                        // Don't invalidate - already expired
                    }
                    else if (_remainingAttempts > 0)
                    {
                        ErrorMessage = $"Incorrect OTP. You have {_remainingAttempts} attempt(s) remaining.";
                    }
                    else
                    {
                        // Exhausted all attempts - invalidate OTP
                        ErrorMessage = "You have exceeded the maximum number of attempts. OTP has been invalidated. Please request a new OTP.";
                        _authService.InvalidateOtp(Email); // ← Set expiry to past, keep OTP in DB
                        ResetOtpState();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"An error occurred while verifying OTP: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteBackToLogin(object parameter)
        {
            // Invalidate OTP when going back (set expiry to past, keep for audit)
            try
            {
                if (!string.IsNullOrWhiteSpace(Email))
                {
                    _authService.InvalidateOtp(Email);
                }
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

        private void ResetOtpState()
        {
            IsOtpSent = false;
            SendOtpButtonText = "Send OTP";
            Otp = string.Empty;
            _remainingAttempts = 5;
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

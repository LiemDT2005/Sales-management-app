using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using SMA.Helper;
using SMA.Models;

namespace SMA.ViewModels.UserViewModels
{
    public class UserProfileViewModel : INotifyPropertyChanged
    {
        private readonly Prn212G3Context _context;
        private User _user;
        private bool _isLoading;

        // Properties
        public User User
        {
            get => _user;
            set
            {
                _user = value;
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

        // Commands
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand CancelCommand { get; }

        public UserProfileViewModel(User user)
        {
            _context = new Prn212G3Context();
            _user = user;
            _isLoading = false;

            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            DeleteCommand = new RelayCommand(ExecuteDelete);
            CancelCommand = new RelayCommand(ExecuteCancel);
        }

        private bool CanExecuteSave(object parameter)
        {
            return !IsLoading;
        }

        private void ExecuteSave(object parameter)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(User.UserName))
            {
                MessageBox.Show("Username is required.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(User.Email))
            {
                MessageBox.Show("Email is required.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!IsValidEmail(User.Email))
            {
                MessageBox.Show("Please enter a valid email address.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(User.CitizenId))
            {
                MessageBox.Show("Citizen ID is required.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(User.Role))
            {
                MessageBox.Show("Role is required.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;

            try
            {
                // Find user in database
                var userInDb = _context.Users.FirstOrDefault(u => u.UserId == User.UserId);

                if (userInDb != null)
                {
                    // Update properties
                    userInDb.UserName = User.UserName;
                    userInDb.Email = User.Email;
                    userInDb.Phone = User.Phone;
                    userInDb.Address = User.Address;
                    userInDb.CitizenId = User.CitizenId;
                    userInDb.Gender = User.Gender;
                    userInDb.Role = User.Role;
                    userInDb.Status = User.Status;

                    // Save to database
                    _context.SaveChanges();

                    MessageBox.Show("User updated successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // Close window
                    CloseWindow(parameter);
                }
                else
                {
                    MessageBox.Show("User not found in database.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving user: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteDelete(object parameter)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete user '{User.UserName}'?\n\nThis action cannot be undone.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;

                try
                {
                    // Find user in database
                    var userInDb = _context.Users.FirstOrDefault(u => u.UserId == User.UserId);

                    if (userInDb != null)
                    {
                        // Check if user has related orders
                        if (userInDb.Orders.Any())
                        {
                            MessageBox.Show(
                                "Cannot delete user with existing orders.\n\nPlease delete or reassign the orders first.",
                                "Cannot Delete",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            return;
                        }

                        // Remove user
                        _context.Users.Remove(userInDb);
                        _context.SaveChanges();

                        MessageBox.Show("User deleted successfully!", "Success",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        // Close window
                        CloseWindow(parameter);
                    }
                    else
                    {
                        MessageBox.Show("User not found in database.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting user: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
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

        // INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SMA.Helper;
using SMA.Models;
using SMA.Services;

namespace SMA.ViewModels.UserViewModels
{
    public class UserManagementViewModel : BaseViewModel
    {
        private readonly Prn212G3Context _context;
        private ObservableCollection<User> _users;
        private ObservableCollection<User> _filteredUsers;
        private User? _selectedUser;
        private string _searchKeyword;
        private bool _isLoading;

        // Properties
        public ObservableCollection<User> Users
        {
            get => _users;
            set
            {
                _users = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<User> FilteredUsers
        {
            get => _filteredUsers;
            set
            {
                _filteredUsers = value;
                OnPropertyChanged();
            }
        }

        public User? SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged();
            }
        }

        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                _searchKeyword = value;
                OnPropertyChanged();
                FilterUsers();
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
        public ICommand AddUserCommand { get; }
        public ICommand ViewProfileCommand { get; }
        public ICommand RefreshCommand { get; }

        public UserManagementViewModel()
        {
            _context = new Prn212G3Context();
            _users = new ObservableCollection<User>();
            _filteredUsers = new ObservableCollection<User>();
            _searchKeyword = string.Empty;
            _isLoading = false;

            AddUserCommand = new RelayCommand(ExecuteAddUser);
            ViewProfileCommand = new RelayCommand(ExecuteViewProfile);
            RefreshCommand = new RelayCommand(_ => LoadUsers());

            // Load users on startup
            LoadUsers();
        }

        private void LoadUsers()
        {
            IsLoading = true;

            try
            {
                // Load all users from database
                var usersFromDb = _context.Users.ToList();

                Users.Clear();
                foreach (var user in usersFromDb)
                {
                    Users.Add(user);
                }

                // Apply filter
                FilterUsers();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading users: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void FilterUsers()
        {
            FilteredUsers.Clear();

            if (string.IsNullOrWhiteSpace(SearchKeyword))
            {
                // No filter - show all users
                foreach (var user in Users)
                {
                    FilteredUsers.Add(user);
                }
            }
            else
            {
                // Filter by keyword (search in UserId, UserName, Email)
                var keyword = SearchKeyword.ToLower();
                var filtered = Users.Where(u =>
                    (u.UserId?.ToLower().Contains(keyword) ?? false) ||
                    (u.UserName?.ToLower().Contains(keyword) ?? false) ||
                    (u.Email?.ToLower().Contains(keyword) ?? false)
                ).ToList();

                foreach (var user in filtered)
                {
                    FilteredUsers.Add(user);
                }
            }
        }

        private void ExecuteAddUser(object parameter)
        {
            var addUserView = new Views.Admin.AddUserView();
            
            // Subscribe to Closed event to refresh list
            addUserView.Closed += (s, e) =>
            {
                // Reload users after adding
                LoadUsers();
            };

            addUserView.ShowDialog();
        }

        private void ExecuteViewProfile(object parameter)
        {
            if (parameter is User user)
            {
                var profileView = new Views.Admin.UserProfileView(user);
                
                // Subscribe to Closed event to refresh list
                profileView.Closed += (s, e) =>
                {
                    // Reload users after editing
                    LoadUsers();
                };

                profileView.ShowDialog();
            }
        }
    }
}

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SMA.Helper;
using SMA.Services;

namespace SMA.ViewModels
{
    public class MainViewModel  : BaseViewModel
    {
        private BaseViewModel _currentViewModel;
        public BaseViewModel CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                _currentViewModel = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand ShowProductCommand { get; }

        private bool _isSidebarCollapsed;
        private string _currentUserName;
        private string _currentUserRole;
        private bool _isUserLoggedIn;
        private bool _isAdmin;
        private bool _isStaff;

        public bool IsSidebarCollapsed
        {
            get => _isSidebarCollapsed;
            set
            {
                _isSidebarCollapsed = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SidebarWidth));
            }
        }

        public double SidebarWidth => IsSidebarCollapsed ? 60 : 260;

        public string CurrentUserName
        {
            get => _currentUserName;
            set
            {
                _currentUserName = value;
                OnPropertyChanged();
            }
        }

        public string CurrentUserRole
        {
            get => _currentUserRole;
            set
            {
                _currentUserRole = value;
                OnPropertyChanged();
            }
        }

        public bool IsUserLoggedIn
        {
            get => _isUserLoggedIn;
            set
            {
                _isUserLoggedIn = value;
                OnPropertyChanged();
            }
        }

        public bool IsAdmin
        {
            get => _isAdmin;
            set
            {
                _isAdmin = value;
                OnPropertyChanged();
            }
        }

        public bool IsStaff
        {
            get => _isStaff;
            set
            {
                _isStaff = value;
                OnPropertyChanged();
            }
        }

        public ICommand ToggleSidebarCommand { get; }
        public ICommand ChangePasswordCommand { get; }
        public ICommand LogoutCommand { get; }

        public MainViewModel()
        {
            _isSidebarCollapsed = false;
            _currentUserName = "Guest";
            _currentUserRole = "Guest";
            _isUserLoggedIn = false;
            _isAdmin = false;
            _isStaff = false;

            ToggleSidebarCommand = new RelayCommand(_ => ToggleSidebar());
            ChangePasswordCommand = new RelayCommand(_ => OpenChangePassword());
            LogoutCommand = new RelayCommand(async _ => await LogoutAsync());

            CurrentViewModel = null;

            ShowProductCommand = new RelayCommand(_ => CurrentViewModel = new ProductVM());
            // Load current user info from session
            LoadCurrentUser();
        }

        private void LoadCurrentUser()
        {
            if (SessionManager.IsLoggedIn)
            {
                CurrentUserName = SessionManager.CurrentUserName;
                CurrentUserRole = SessionManager.CurrentUserRole;
                IsUserLoggedIn = true;
                IsAdmin = SessionManager.IsAdmin;
                IsStaff = SessionManager.IsStaff;
            }
            else
            {
                CurrentUserName = "Guest";
                CurrentUserRole = "Guest";
                IsUserLoggedIn = false;
                IsAdmin = false;
                IsStaff = false;
            }
        }
        

        private void ToggleSidebar()
        {
            IsSidebarCollapsed = !IsSidebarCollapsed;
        }

        private void OpenChangePassword()
        {
            var changePasswordView = new Views.Auth.ChangePasswordView();
            changePasswordView.ShowDialog();
        }

        private async Task LogoutAsync()
        {
            var result = MessageBox.Show("Are you sure you want to logout?", "Logout",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Clear session first
                    SessionManager.Logout();

                    // Find current MainWindow
                    MainWindow? currentMainWindow = null;
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window is MainWindow mw)
                        {
                            currentMainWindow = mw;
                            break;
                        }
                    }

                    if (currentMainWindow != null)
                    {
                        // Set shutdown mode to prevent app exit when MainWindow closes
                        Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                        
                        // STRATEGY: Hide first to make it invisible immediately
                        currentMainWindow.Hide();
                        
                        // Force UI update to process Hide
                        await Application.Current.Dispatcher.InvokeAsync(() => { },
                            System.Windows.Threading.DispatcherPriority.Render);
                        
                        // Small delay to ensure Hide is rendered
                        await Task.Delay(100);
                        
                        // Now close the hidden window
                        var tcs = new TaskCompletionSource<bool>();
                        currentMainWindow.Closed += (s, e) => tcs.TrySetResult(true);
                        currentMainWindow.Close();
                        
                        // Wait for close with timeout
                        await Task.WhenAny(tcs.Task, Task.Delay(1000));
                        
                        // Process any remaining events
                        await Application.Current.Dispatcher.InvokeAsync(() => { },
                            System.Windows.Threading.DispatcherPriority.Background);
                    }

                    // Small delay for clean state
                    await Task.Delay(50);

                    // Now show Login window - MainWindow is hidden and closed
                    var loginWindow = new Views.Auth.Login();
                    var loginResult = loginWindow.ShowDialog();

                    // After login dialog closes, check if user logged in successfully
                    if (SessionManager.IsLoggedIn)
                    {
                        // User logged in successfully -> Create NEW MainWindow
                        var newMainWindow = new MainWindow();
                        
                        // Restore normal shutdown mode
                        Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                        Application.Current.MainWindow = newMainWindow;
                        
                        newMainWindow.Show();
                    }
                    else
                    {
                        // User closed login without logging in -> Shutdown application
                        Application.Current.Shutdown();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error during logout: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    
                    // Restore shutdown mode in case of error
                    Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                }
            }
        }

    }
}

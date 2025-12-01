using Microsoft.EntityFrameworkCore;
using SMA.Helper;
using SMA.Models;
using SMA.Views.Orders;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using SMA;

namespace SMA.ViewModels
{
    public class OrderListViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Order> _orders = new();
        private ObservableCollection<OrderViewModel> _filteredOrders = new();
        private Order? _selectedOrder;
        private string _searchKeyword = string.Empty;
        private DateTime? _selectedDate;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<Order> Orders
        {
            get => _orders;
            set { _orders = value; OnPropertyChanged(); UpdateFilteredOrders(); }
        }

        public ObservableCollection<OrderViewModel> FilteredOrders
        {
            get => _filteredOrders;
            set { _filteredOrders = value; OnPropertyChanged(); }
        }

        public Order? SelectedOrder
        {
            get => _selectedOrder;
            set { _selectedOrder = value; OnPropertyChanged(); }
        }

        public string SearchKeyword
        {
            get => _searchKeyword;
            set { _searchKeyword = value; OnPropertyChanged(); }
        }

        public DateTime? SelectedDate
        {
            get => _selectedDate;
            set { _selectedDate = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand SearchCommand { get; }
        public ICommand CreateCommand { get; }
        public ICommand ViewCommand { get; }
        public ICommand UpdateCommand { get; }

        public OrderListViewModel()
        {
            LoadAllOrders();

            SearchCommand = new RelayCommand(ExecuteSearch);
            CreateCommand = new RelayCommand(ExecuteCreate);
            ViewCommand = new RelayCommand(ExecuteView, CanExecuteSelected);
            UpdateCommand = new RelayCommand(ExecuteUpdate, CanExecuteSelected);
        }

        private void LoadAllOrders()
        {
            using var db = new Prn212G3Context();
            var list = db.Orders
                .Include(o => o.Customer)
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            Orders = new ObservableCollection<Order>(list);
            UpdateFilteredOrders();
        }

        private void UpdateFilteredOrders()
        {
            var filtered = Orders.Select(o => new OrderViewModel
            {
                OrderId = o.OrderId,
                StaffName = o.User?.UserName ?? string.Empty,
                CustomerName = o.Customer?.CustomerName ?? string.Empty,
                TotalPrice = o.TotalPrice ?? 0,
                PointReceived = o.PointReceived ?? 0,
                PointUsed = o.PointUsed ?? 0,
                CreatedAt = o.CreatedAt ?? System.DateTime.Now
            }).AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                filtered = filtered.Where(o =>
                    o.CustomerName.Contains(SearchKeyword, System.StringComparison.OrdinalIgnoreCase) ||
                    o.StaffName.Contains(SearchKeyword, System.StringComparison.OrdinalIgnoreCase));
            }

            if (SelectedDate.HasValue)
            {
                filtered = filtered.Where(o => o.CreatedAt.Date == SelectedDate.Value.Date);
            }

            FilteredOrders = new ObservableCollection<OrderViewModel>(filtered);
        }

        private void ExecuteSearch(object? obj)
        {
            UpdateFilteredOrders();
        }

        private void ExecuteCreate(object? obj)
        {
            var mainWindow = System.Windows.Application.Current.Windows
                .OfType<MainWindow>()
                .FirstOrDefault() ?? System.Windows.Application.Current.MainWindow as MainWindow;
            if (mainWindow == null) return;

            var mainContent = mainWindow.FindName("MainContent") as System.Windows.Controls.ContentControl;
            if (mainContent != null)
            {
                mainContent.Content = new CreateOrderView();
            }
        }

        private void ExecuteView(object? obj)
        {
            if (obj is not OrderViewModel orderView) return;

            var mainWindow = System.Windows.Application.Current.Windows
                .OfType<MainWindow>()
                .FirstOrDefault() ?? System.Windows.Application.Current.MainWindow as MainWindow;
            if (mainWindow == null) return;

            var mainContent = mainWindow.FindName("MainContent") as System.Windows.Controls.ContentControl;
            if (mainContent != null)
            {
                mainContent.Content = new OrderDetailView(orderView.OrderId);
            }
        }

        private void ExecuteUpdate(object? obj)
        {
            ExecuteView(obj);
        }

        private bool CanExecuteSelected(object? obj) => obj != null;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

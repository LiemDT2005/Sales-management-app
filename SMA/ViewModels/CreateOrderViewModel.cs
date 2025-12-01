using Microsoft.EntityFrameworkCore;
using SMA.Helper;
using SMA.Models;
using SMA.Services;
using SMA.Views.Orders;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SMA.ViewModels
{
    public class CreateOrderViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Product> Products { get; set; }
        public ObservableCollection<Product> FilteredProducts { get; set; }
        public ObservableCollection<OrderDetailItem> OrderItems { get; set; }
        public ObservableCollection<Customer> Customers { get; set; }
        public ObservableCollection<Category> Categories { get; set; }

        private Product _selectedProduct;
        public Product SelectedProduct
        {
            get => _selectedProduct;
            set { _selectedProduct = value; OnPropertyChanged(); }
        }

        private string _productSearchText;
        public string ProductSearchText
        {
            get => _productSearchText;
            set { _productSearchText = value; OnPropertyChanged(); }
        }

        private int? _selectedCategoryId;
        public int? SelectedCategoryId
        {
            get => _selectedCategoryId;
            set { _selectedCategoryId = value; OnPropertyChanged(); FilterProducts(); }
        }

        private string _selectedCustomerId;
        public string SelectedCustomerId
        {
            get => _selectedCustomerId;
            set { _selectedCustomerId = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectedCustomerPoints)); UpdateTotal(); System.Windows.Input.CommandManager.InvalidateRequerySuggested(); }
        }

        public int? SelectedCustomerPoints
        {
            get
            {
                if (string.IsNullOrEmpty(SelectedCustomerId)) return 0;
                var customer = Customers.FirstOrDefault(c => c.CustomerId == SelectedCustomerId);
                return customer?.Point ?? 0;
            }
        }

        private bool _usePoints;
        public bool UsePoints
        {
            get => _usePoints;
            set { _usePoints = value; OnPropertyChanged(); UpdateTotal(); }
        }

        private decimal _totalPrice;
        public decimal TotalPrice
        {
            get => _totalPrice;
            set { _totalPrice = value; OnPropertyChanged(); }
        }

        private decimal _subTotal;
        public decimal SubTotal
        {
            get => _subTotal;
            set { _subTotal = value; OnPropertyChanged(); }
        }

        private decimal _pointDiscount;
        public decimal PointDiscount
        {
            get => _pointDiscount;
            set { _pointDiscount = value; OnPropertyChanged(); }
        }

        private int _pointsReceived;
        public int PointsReceived
        {
            get => _pointsReceived;
            set { _pointsReceived = value; OnPropertyChanged(); }
        }

        private int _pointsUsed;
        public int PointsUsed
        {
            get => _pointsUsed;
            set { _pointsUsed = value; OnPropertyChanged(); }
        }

        public ICommand AddProductCommand { get; }
        public ICommand IncreaseQtyCommand { get; }
        public ICommand DecreaseQtyCommand { get; }
        public ICommand RemoveProductCommand { get; }
        public ICommand SearchProductCommand { get; }
        public ICommand ConfirmOrderCommand { get; }
        public ICommand GoBackCommand { get; }

        public event EventHandler? RequestNavigationBack;

        public CreateOrderViewModel()
        {
            Products = new ObservableCollection<Product>();
            FilteredProducts = new ObservableCollection<Product>();
            OrderItems = new ObservableCollection<OrderDetailItem>();
            Customers = new ObservableCollection<Customer>();
            Categories = new ObservableCollection<Category>();

            AddProductCommand = new RelayCommand(AddProduct);
            IncreaseQtyCommand = new RelayCommand(IncreaseQty);
            DecreaseQtyCommand = new RelayCommand(DecreaseQty);
            RemoveProductCommand = new RelayCommand(RemoveProduct);
            SearchProductCommand = new RelayCommand(SearchProduct);
            // Always enable the Confirm button; validations happen inside ConfirmOrder
            ConfirmOrderCommand = new RelayCommand(ConfirmOrder);
            GoBackCommand = new RelayCommand(GoBack);

            LoadProducts();
            LoadCustomers();
            LoadCategories();

            // Keep totals and command state in sync when order items change
            OrderItems.CollectionChanged += OnOrderItemsCollectionChanged;
        }

        private void OnOrderItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateTotal();
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }

        // 🔹 Load danh sách sản phẩm từ DB
        private void LoadProducts()
        {
            using var db = new Prn212G3Context();
            var list = db.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive != false) // include null and true
                .ToList();

            Products.Clear();
            foreach (var p in list)
                Products.Add(p);

            FilterProducts();
        }

        private void LoadCustomers()
        {
            using var db = new Prn212G3Context();
            var list = db.Customers.ToList();
            Customers = new ObservableCollection<Customer>(list);

            if (list.Any())
                SelectedCustomerId = list.First().CustomerId;
        }

        private void LoadCategories()
        {
            using var db = new Prn212G3Context();
            var list = db.Categories.ToList();
            Categories.Clear();
            Categories.Add(new Category { CategoryId = 0, CategoryName = "All" }); // Add "All" option
            foreach (var c in list)
                Categories.Add(c);

            // Set default category to "All"
            SelectedCategoryId = 0;
        }

        // 🔍 Tìm kiếm sản phẩm
        private void SearchProduct(object obj)
        {
            FilterProducts();
        }

        private void FilterProducts()
        {
            if (Products == null)
            {
                FilteredProducts = new ObservableCollection<Product>();
                OnPropertyChanged(nameof(FilteredProducts));
                return;
            }

            var filtered = Products.AsEnumerable();

            // Filter by category
            if (SelectedCategoryId.HasValue && SelectedCategoryId.Value > 0)
            {
                filtered = filtered.Where(p => p.CategoryId == SelectedCategoryId.Value);
            }

            // Filter by search text
            if (!string.IsNullOrWhiteSpace(ProductSearchText))
            {
                filtered = filtered.Where(p =>
                    p.ProductName.Contains(ProductSearchText, StringComparison.OrdinalIgnoreCase) ||
                    (p.Category?.CategoryName?.Contains(ProductSearchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            FilteredProducts = new ObservableCollection<Product>(filtered);
            OnPropertyChanged(nameof(FilteredProducts));
        }

        // ➕ Thêm sản phẩm vào danh sách Order
        private void AddProduct(object obj)
        {
            if (obj is Product product)
            {
                var existing = OrderItems.FirstOrDefault(o => o.ProductId == product.ProductId);
                if (existing != null)
                {
                    if (existing.Quantity < product.StockQuantity)
                    {
                        existing.Quantity++;
                    }
                    else
                    {
                        MessageBox.Show($"Insufficient stock! Available: {product.StockQuantity}");
                    }
                }
                else
                {
                    OrderItems.Add(new OrderDetailItem
                    {
                        ProductId = product.ProductId,
                        ProductName = product.ProductName,
                        UnitPrice = product.Price ?? 0,
                        Quantity = 1
                    });
                }

                UpdateTotal();
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }

        // 🔺 Tăng số lượng
        private void IncreaseQty(object obj)
        {
            if (obj is OrderDetailItem item)
            {
                using var db = new Prn212G3Context();
                var product = db.Products.FirstOrDefault(p => p.ProductId == item.ProductId);
                if (product != null && item.Quantity < product.StockQuantity)
                {
                    item.Quantity++;
                    UpdateTotal();
                }
                else
                {
                    MessageBox.Show($"Insufficient stock! Available: {product?.StockQuantity ?? 0}");
                }
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }

        // 🔻 Giảm số lượng
        private void DecreaseQty(object obj)
        {
            if (obj is OrderDetailItem item && item.Quantity > 1)
            {
                item.Quantity--;
                UpdateTotal();
            }
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }

        // 🗑️ Xóa sản phẩm khỏi đơn hàng
        private void RemoveProduct(object obj)
        {
            if (obj is OrderDetailItem item)
            {
                OrderItems.Remove(item);
                UpdateTotal();
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }

        // 🧮 Tính tổng tiền (gồm áp dụng điểm thưởng)
        private void UpdateTotal()
        {
            SubTotal = OrderItems.Sum(i => i.Total);

            if (UsePoints && !string.IsNullOrEmpty(SelectedCustomerId))
            {
                var customerPoints = SelectedCustomerPoints ?? 0;

                // Max 10% of order value, 1 point = 1₫
                var maxDiscount = SubTotal / 10m;
                var pointsCanUse = Math.Min(customerPoints, (int)maxDiscount);

                PointDiscount = pointsCanUse;
                PointsUsed = pointsCanUse;
                TotalPrice = SubTotal - PointDiscount;

                // When applying points, no points received
                PointsReceived = 0;
            }
            else
            {
                PointDiscount = 0;
                PointsUsed = 0;
                TotalPrice = SubTotal;

                // Not applying points: 100₫ = 1 point
                PointsReceived = (int)(TotalPrice / 100m);
            }

            OnPropertyChanged(nameof(SelectedCustomerPoints));
        }

        // ✅ Xác nhận đơn hàng
        private void ConfirmOrder(object obj)
        {
            if (string.IsNullOrEmpty(SelectedCustomerId))
            {
                MessageBox.Show("Please select a customer!");
                return;
            }

            if (!OrderItems.Any())
            {
                MessageBox.Show("Please add at least one product!");
                return;
            }

            using var db = new Prn212G3Context();
            using var transaction = db.Database.BeginTransaction();

            try
            {
                // Tạo đơn hàng mới
                var order = new Order
                {
                    CustomerId = SelectedCustomerId,
                    UserId = GetCurrentUserId(), // TODO: Lấy từ session
                    TotalPrice = TotalPrice,
                    PointReceived = PointsReceived,
                    PointUsed = PointsUsed,
                    CreatedAt = DateTime.Now
                };

                db.Orders.Add(order);
                db.SaveChanges(); // Lưu để có OrderId

                // Thêm OrderDetails và cập nhật tồn kho
                foreach (var item in OrderItems)
                {
                    var product = db.Products.FirstOrDefault(p => p.ProductId == item.ProductId);
                    if (product == null || product.StockQuantity < item.Quantity)
                    {
                        MessageBox.Show($"Product {item.ProductName} has insufficient stock!");
                        transaction.Rollback();
                        return;
                    }

                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.OrderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity
                    };
                    db.OrderDetails.Add(orderDetail);

                    // Cập nhật tồn kho
                    product.StockQuantity -= item.Quantity;
                }

                // Cập nhật điểm khách hàng
                var customer = db.Customers.FirstOrDefault(c => c.CustomerId == SelectedCustomerId);
                if (customer != null)
                {
                    // Trừ điểm đã dùng
                    customer.Point = (customer.Point ?? 0) - PointsUsed;
                    // Cộng điểm nhận được
                    customer.Point = (customer.Point ?? 0) + PointsReceived;
                }

                db.SaveChanges();
                transaction.Commit();

                Task.Run(async () =>
                {
                    var sendEmailUtility = new SendInvoiceEmail();
                    await sendEmailUtility.SendEmailToCustomer(customer.CustomerId, order.OrderId);
                });

                MessageBox.Show($"Order created successfully!\nOrder ID: {order.OrderId}");

                // Reset form or navigate to OrderList
                OrderItems.Clear();
                UpdateTotal();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private bool CanConfirm(object obj)
        {
            return OrderItems != null && OrderItems.Any() && !string.IsNullOrEmpty(SelectedCustomerId);
        }

        private string GetCurrentUserId()
        {
            // Lấy UserId từ SessionManager
            if (SessionManager.CurrentUser != null)
            {
                return SessionManager.CurrentUser.UserId;
            }

            // Fallback: nếu chưa login (không nên xảy ra)
            MessageBox.Show("No user logged in. Please login first.");
            return string.Empty;
        }

        // 🧾 Load chi tiết đơn hàng có sẵn (nếu đang chỉnh sửa đơn)
        public void LoadOrderDetails(int orderId)
        {
            using var db = new Prn212G3Context();
            var order = db.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefault(o => o.OrderId == orderId);

            if (order == null) return;

            SelectedCustomerId = order.CustomerId;
            UsePoints = order.PointUsed > 0;

            OrderItems.Clear();
            foreach (var detail in order.OrderDetails)
            {
                OrderItems.Add(new OrderDetailItem
                {
                    OrderDetailId = detail.OrderDetailId,
                    ProductId = detail.ProductId,
                    ProductName = detail.Product.ProductName,
                    UnitPrice = detail.Product.Price ?? 0,
                    Quantity = detail.Quantity ?? 0
                });
            }

            UpdateTotal();
        }

        private void GoBack(object obj)
        {
            var mainWindow = Application.Current.Windows
                .OfType<MainWindow>()
                .FirstOrDefault() as MainWindow ?? Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                var mainContent = mainWindow.FindName("MainContent") as System.Windows.Controls.ContentControl;
                if (mainContent != null)
                {
                    mainContent.Content = new OrderList();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // 🔸 Lớp hiển thị chi tiết sản phẩm trong đơn hàng (ViewModel con)
    public class OrderDetailItem : INotifyPropertyChanged
    {
        public int OrderDetailId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal UnitPrice { get; set; }

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(); OnPropertyChanged(nameof(Total)); }
        }

        public decimal Total => UnitPrice * Quantity;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

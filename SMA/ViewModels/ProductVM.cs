using SMA.Models;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using SMA.Helper;
using System.Windows;
using System.IO;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using SMA.Services;

namespace SMA.ViewModels
{
    public class ProductVM : BaseViewModel, IStockObserver
    {
        private readonly Prn212G3Context _db;

        public ProductVM()
        {
            _db = new Prn212G3Context();

            if (SessionManager.IsAdmin)
            {
                StockMonitor.Instance.Subscribe(this);
                CheckLowStock();
            }

            LoadProducts();
            LoadCategories();

            AddCommand = new RelayCommand(_ => AddProduct(), _ => CanCreate);
            DeleteCommand = new RelayCommand(_ => DeleteProduct(), _ => CanDelete);
            SearchCommand = new RelayCommand(_ => SearchProducts());
            FilterCommand = new RelayCommand(_ => FilterProduct());
            ClearFilterCommand = new RelayCommand(_ => ClearFilter());
            SelectImageCommand = new RelayCommand(_ => SelectImage());
            SaveCommand = new RelayCommand(_ => SaveProduct(), _ => CanUpdate);
            CancelCommand = new RelayCommand(_ => CancelEdit());
        }

        // Kiểm tra stock thấp và thông báo qua Observer pattern
        private void CheckLowStock()
        {
            StockMonitor.Instance.CheckLowStock(_db);
        }

        private static bool _isWarningWindowOpen = false;

        // Implement IStockObserver gọi khi <10
        public void OnLowStockDetected(List<Product> lowStockProducts)
        {
            if (_isWarningWindowOpen)
            {
                return; 
            }

            var existingWindow = Application.Current.Windows.OfType<Views.Admin.StockWarningWindow>().FirstOrDefault();
            if (existingWindow != null && existingWindow.IsVisible)
            {
                return; 
            }

            _isWarningWindowOpen = true;

            try
            {
                var result = MessageBox.Show(
                    $"Có {lowStockProducts.Count} sản phẩm đang dưới 10 sản phẩm trong kho.\n\nBạn có muốn nhập thêm hàng không?",
                    "Cảnh báo tồn kho thấp",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var viewModel = new StockWarningViewModel(lowStockProducts);
                        var window = new Views.Admin.StockWarningWindow(viewModel);
                        
                        // Đăng ký event để reset flag khi window đóng
                        window.Closed += (s, e) => 
                        { 
                            _isWarningWindowOpen = false;
                            StockMonitor.Instance.ResetNotifiedProducts();
                        };
                        
                        // ShowDialog và reload products sau khi đóng
                        var dialogResult = window.ShowDialog();
                        
                        // Reload products để cập nhật số lượng mới (nếu có thay đổi)
                        if (dialogResult == true)
                        {
                            LoadProducts();
                        }
                    });
                }
                else
                {
                    _isWarningWindowOpen = false;
                    StockMonitor.Instance.ResetNotifiedProducts();
                }
            }
            catch
            {
                _isWarningWindowOpen = false;
                StockMonitor.Instance.ResetNotifiedProducts();
            }
        }

        // Data sources
        public ObservableCollection<Product> Products { get; } = new();
        public ObservableCollection<Category> Categories { get; } = new();

        // Selected row
        private Product _selectedProduct;
        public Product SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                _selectedProduct = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEditing));
                OnPropertyChanged(nameof(CanDelete));
                OnPropertyChanged(nameof(CanUpdate));

                // Update editing properties when selection changes
                if (value != null)
                {
                    EditingProductName = value.ProductName;
                    EditingCategoryId = value.CategoryId;
                    EditingDescription = value.Description ?? string.Empty;
                    EditingStockQuantity = value.StockQuantity;
                    EditingPrice = value.Price ?? 0;
                    EditingImageUrl = value.ImageUrl ?? string.Empty;
                    EditingIsActive = value.IsActive ?? true;

                    IsFormEnabled = SessionManager.IsAdmin;
                }
                else
                {
                    // Clear editing fields when no product is selected (ready for new product)
                    EditingProductName = string.Empty;
                    EditingCategoryId = Categories.FirstOrDefault()?.CategoryId ?? 1;
                    EditingDescription = string.Empty;
                    EditingStockQuantity = 0;
                    EditingPrice = 0;
                    EditingImageUrl = string.Empty;
                    EditingIsActive = true;

                    IsFormEnabled = false;
                }
            }
        }

        private bool _isFormEnabled;
        public bool IsFormEnabled
        {
            get => _isFormEnabled;
            set { _isFormEnabled = value; OnPropertyChanged(); }
        }

        // Editing properties for form
        private string _editingProductName = string.Empty;
        public string EditingProductName
        {
            get => _editingProductName;
            set { _editingProductName = value; OnPropertyChanged(); }
        }

        private int _editingCategoryId;
        public int EditingCategoryId
        {
            get => _editingCategoryId;
            set { _editingCategoryId = value; OnPropertyChanged(); }
        }

        private string _editingDescription = string.Empty;
        public string EditingDescription
        {
            get => _editingDescription;
            set { _editingDescription = value; OnPropertyChanged(); }
        }

        private int _editingStockQuantity;
        public int EditingStockQuantity
        {
            get => _editingStockQuantity;
            set { _editingStockQuantity = value; OnPropertyChanged(); }
        }

        private decimal _editingPrice;
        public decimal EditingPrice
        {
            get => _editingPrice;
            set { _editingPrice = value; OnPropertyChanged(); }
        }

        private string _editingImageUrl = string.Empty;
        public string EditingImageUrl
        {
            get => _editingImageUrl;
            set { _editingImageUrl = value; OnPropertyChanged(); }
        }

        private bool _editingIsActive = true;
        public bool EditingIsActive
        {
            get => _editingIsActive;
            set { _editingIsActive = value; OnPropertyChanged(); }
        }

        public bool IsEditing => SelectedProduct != null;


        // Kiểm tra quyền 
        public bool CanCreate => SessionManager.IsAdmin;
        public bool CanUpdate => SessionManager.IsAdmin && SelectedProduct != null;
        public bool CanDelete => SelectedProduct != null && SessionManager.IsAdmin;


        // 🔓 Public để XAML binding không lỗi
        private string _keyword;
        public string Keyword
        {
            get => _keyword;
            set { _keyword = value; OnPropertyChanged(); }
        }

        private int? _selectedCategoryId;
        public int? SelectedCategoryId
        {
            get => _selectedCategoryId;
            set { _selectedCategoryId = value; OnPropertyChanged(); }
        }

        // Status bar
        private string _status;
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand AddCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand FilterCommand { get; }
        public ICommand ClearFilterCommand { get; }
        public ICommand SelectImageCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        private void LoadProducts()
        {
            Products.Clear();
            // Load products with Category navigation property
            var list = _db.Products.Include(p => p.Category).ToList();
            foreach (var p in list) Products.Add(p);
            Status = $"Total: {Products.Count}";
            
            // Không kiểm tra stock lại khi reload (tránh popup liên tục)
            // Chỉ kiểm tra khi khởi tạo ProductVM lần đầu
        }

        private void LoadCategories()
        {
            Categories.Clear();
            foreach (var c in _db.Categories.ToList()) Categories.Add(c);
        }

        private void AddProduct()
        {
            // Vào chế độ tạo mới: không Save, không LoadProducts ngay
            SelectedProduct = new Product(); // ProductId == 0 => đang tạo mới
            EditingProductName = string.Empty;
            EditingCategoryId = Categories.FirstOrDefault()?.CategoryId ?? 1;
            EditingDescription = string.Empty;
            EditingStockQuantity = 0;
            EditingPrice = 0;
            EditingImageUrl = string.Empty;
            EditingIsActive = true;

            IsFormEnabled = true;
            Status = "Creating a new product...";
        }

        private void SaveProduct()
        {
            if (SelectedProduct == null) return;

            var errors = ProductInputValidator.ValidateAll(
            name: EditingProductName,
            categoryId: EditingCategoryId,
            stockQuantity: EditingStockQuantity,
            price: EditingPrice,
            imageFileName: EditingImageUrl,
            categories: Categories,
            description: EditingDescription
        );

            if (errors.Count > 0)
            {
                var msg = string.Join("\n• ", errors);
                MessageBox.Show("Please fix the following errors:\n\n• " + msg,
                                "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                Status = "Validation failed.";
                return;
            }

            try
            {
                var isNew = SelectedProduct.ProductId == 0; // chưa có trong DB

                if (!isNew)
                {
                    var confirmUpdate = MessageBox.Show(
                        $"Are you sure you want to update the product '{EditingProductName}'?",
                        "Confirm Update",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (confirmUpdate != MessageBoxResult.Yes)
                    {
                        Status = "Update cancelled by user.";
                        return;
                    }
                }

                if (isNew)
                {
                    var p = new Product
                    {
                        ProductName = EditingProductName,
                        CategoryId = EditingCategoryId,
                        Description = EditingDescription,
                        StockQuantity = EditingStockQuantity,
                        Price = EditingPrice,
                        ImageUrl = EditingImageUrl,
                        IsActive = EditingIsActive,
                        CreatedAt = DateTime.Now
                    };
                    _db.Products.Add(p);
                    _db.SaveChanges();

                    // Refresh + reselect item vừa thêm
                    LoadProducts();
                    SelectedProduct = Products.FirstOrDefault(x => x.ProductId == p.ProductId);
                }
                else
                {
                    // Update
                    SelectedProduct.ProductName = EditingProductName;
                    SelectedProduct.CategoryId = EditingCategoryId;
                    SelectedProduct.Description = EditingDescription;
                    SelectedProduct.StockQuantity = EditingStockQuantity;
                    SelectedProduct.Price = EditingPrice;
                    SelectedProduct.ImageUrl = EditingImageUrl;
                    SelectedProduct.IsActive = EditingIsActive;
                    SelectedProduct.UpdatedAt = DateTime.Now;

                    _db.Products.Update(SelectedProduct);
                    _db.SaveChanges();

                    var id = SelectedProduct.ProductId;
                    LoadProducts();
                    SelectedProduct = Products.FirstOrDefault(x => x.ProductId == id);
                }

                IsFormEnabled = false; // đóng chế độ edit sau khi lưu
                Status = "Product saved successfully!";
            }
            catch (Exception ex)
            {
                Status = $"Error saving product: {ex.Message}";
            }
        }

        private void CancelEdit()
        {
            if (SelectedProduct != null)
            {
                if (SelectedProduct.ProductId == 0)
                {
                    // Hủy thêm mới
                    SelectedProduct = null;
                    IsFormEnabled = false;
                    Status = "New product creation cancelled.";
                    return;
                }

                // Hủy edit: reload entity từ DB
                _db.Entry(SelectedProduct).Reload();
                var current = SelectedProduct;
                SelectedProduct = null;
                SelectedProduct = current;
            }
            IsFormEnabled = false;
            Status = "Changes cancelled.";
        }

        private const string DefaultImagePath = "Assets/Products/";

        private void SelectImage()
        {
            try
            {
 
                // Open file dialog to select image
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Select Product Image",
                    Filter = "Image files (*.jpg;*.jpeg;*.png;*.gif;*.bmp)|*.jpg;*.jpeg;*.png;*.gif;(1);*.bmp|All files (*.*)|*.*",
                    FilterIndex = 1,
                    RestoreDirectory = true
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var selectedFilePath = openFileDialog.FileName;
                    var fileName = Path.GetFileName(selectedFilePath);

                    // Get the destination directory
                    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    var destinationDir = Path.Combine(baseDir, DefaultImagePath);

                    // Create directory if it doesn't exist
                    if (!Directory.Exists(destinationDir))
                    {
                        Directory.CreateDirectory(destinationDir);
                    }

                    // Build destination file path
                    var destinationPath = Path.Combine(destinationDir, fileName);

                    // If file already exists
                    if (File.Exists(destinationPath))
                    {
                        var result = MessageBox.Show(
                            $"File '{fileName}' already exists. Do you want to add it?",
                            "File Exists",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            
                        }

                        if (result == MessageBoxResult.No)
                        {
                            return;
                        }
                    }

                    // Copy file to Assets/Products folder
                    File.Copy(selectedFilePath, destinationPath, true);

                    // Update the editing image URL with just the filename
                    EditingImageUrl = fileName.Replace(".jpg", "") + "(1).jpg";
                    Status = $"Image '{fileName}' copied to Assets/Products successfully!";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error selecting/copying image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Status = $"Error: {ex.Message}";
            }
        }

        private void DeleteProduct()
        {
            if (SelectedProduct == null) return;

            // Kiểm tra quyền: chỉ admin mới được delete
            if (!SessionManager.IsAdmin)
            {
                MessageBox.Show("You do not have permission to delete products. Only administrators can delete products.",
                    "Permission Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                Status = "Delete operation denied: Staff role does not have delete permission.";
                return;
            }

            // Xác nhận trước khi xóa
            var confirmResult = MessageBox.Show(
                $"Are you sure you want to delete product '{SelectedProduct.ProductName}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmResult == MessageBoxResult.Yes)
            {
                try
                {
                    _db.Products.Remove(SelectedProduct);
                    _db.SaveChanges();
                    LoadProducts();
                    SelectedProduct = null;
                    Status = "Product deleted successfully.";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting product: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Status = $"Error: {ex.Message}";
                }
            }
        }

        private void SearchProducts()
        {
            var q = _db.Products.AsQueryable();
            if (!string.IsNullOrWhiteSpace(Keyword))
                q = q.Where(p => p.ProductName.Contains(Keyword));

            Products.Clear();
            foreach (var p in q.ToList()) Products.Add(p);
            Status = $"Found: {Products.Count}";
        }

        private void FilterProduct()
        {
            var q = _db.Products.Include(p => p.Category).AsQueryable();

            if (SelectedCategoryId.HasValue)
                q = q.Where(p => p.CategoryId == SelectedCategoryId.Value);

            Products.Clear();
            foreach (var p in q.ToList()) Products.Add(p);
            Status = SelectedCategoryId.HasValue ? $"Filtered: {Products.Count} products" : $"Total: {Products.Count} products";
        }

        private void ClearFilter()
        {
            SelectedCategoryId = null;
            LoadProducts();
            Status = $"Total: {Products.Count} products";
        }

        /// <summary>
        /// Cleanup khi ProductVM bị dispose
        /// </summary>
        ~ProductVM()
        {
            // Hủy đăng ký observer
            StockMonitor.Instance.Unsubscribe(this);
        }
    }
}

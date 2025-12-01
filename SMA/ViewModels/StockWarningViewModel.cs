using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using SMA.Helper;
using System.Windows;
using SMA.Models;
using System;

namespace SMA.ViewModels
{
    /// <summary>
    /// ViewModel cho Stock Warning Window
    /// </summary>
    public class StockWarningViewModel : BaseViewModel
    {
        private readonly Prn212G3Context _db;
        private readonly List<Product> _lowStockProducts;

        public StockWarningViewModel(List<Product> lowStockProducts)
        {
            _db = new Prn212G3Context();
            _lowStockProducts = lowStockProducts ?? new List<Product>();

            // Khởi tạo danh sách với số lượng nhập vào = 0
            foreach (var product in _lowStockProducts)
            {
                var item = new StockUpdateItem
                {
                    Product = product,
                    QuantityToAdd = 0
                };
                StockUpdateItems.Add(item);
            }

            SaveCommand = new RelayCommand(_ => SaveStockUpdates());
            CancelCommand = new RelayCommand(_ => Cancel());
        }

        public ObservableCollection<StockUpdateItem> StockUpdateItems { get; } = new();

        public string WarningMessage => $"There are {_lowStockProducts.Count} products with less than 10 in stock. Do you want to add more?";

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        private void SaveStockUpdates()
        {
            try
            {
                var hasValidUpdates = StockUpdateItems.Any(item => item.QuantityToAdd > 0);
                
                if (!hasValidUpdates)
                {
                    MessageBox.Show("Please enter the quantity of items to add to the warehouse (quantity must be greater than 0).", "Notification",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                IsLoading = true;

                var updatedCount = 0;
                foreach (var item in StockUpdateItems)
                {
                    if (item.QuantityToAdd > 0)
                    {
                        // Tìm product trong database
                        var product = _db.Products.FirstOrDefault(p => p.ProductId == item.Product.ProductId);
                        if (product != null)
                        {
                            product.StockQuantity += item.QuantityToAdd;
                            product.UpdatedAt = DateTime.Now;
                            updatedCount++;
                        }
                    }
                }

                if (updatedCount > 0)
                {
                    _db.SaveChanges();
                    MessageBox.Show($"Successfully updated the quantity of {updatedCount} product!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Đóng window với DialogResult = true để reload products
                    CloseWindow(true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating item quantity: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void Cancel()
        {
            CloseWindow(false);
        }

        private void CloseWindow(bool dialogResult = true)
        {
            // Tìm và đóng window
            foreach (Window window in Application.Current.Windows)
            {
                if (window is Views.Admin.StockWarningWindow stockWindow && stockWindow.DataContext == this)
                {
                    stockWindow.DialogResult = dialogResult;
                    stockWindow.Close();
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Item để quản lý cập nhật stock cho từng sản phẩm
    /// </summary>
    public class StockUpdateItem : BaseViewModel
    {
        private Product _product;
        private int _quantityToAdd;

        public Product Product
        {
            get => _product;
            set { _product = value; OnPropertyChanged(); }
        }

        public int QuantityToAdd
        {
            get => _quantityToAdd;
            set
            {
                // Đảm bảo số lượng không âm
                _quantityToAdd = Math.Max(0, value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(NewStockQuantity));
            }
        }

        public int CurrentStock => Product?.StockQuantity ?? 0;
        public int NewStockQuantity => CurrentStock + QuantityToAdd;
        public string ProductName => Product?.ProductName ?? string.Empty;
    }
}

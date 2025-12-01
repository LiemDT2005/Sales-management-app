using Microsoft.EntityFrameworkCore;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SMA.Helper;
using SMA.Models;
using SMA.Views.Orders;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace SMA.ViewModels
{
    public class OrderDetailViewModel : INotifyPropertyChanged
    {
        private int _orderId;
        private ObservableCollection<OrderDetailEditItem> _orderItems = new();
        private HashSet<int> _removedDetailIds = new HashSet<int>();

        private int _orderIdProp;
        public int OrderId
        {
            get => _orderIdProp;
            set { _orderIdProp = value; OnPropertyChanged(); }
        }

        private string _customerName = string.Empty;
        public string CustomerName
        {
            get => _customerName;
            set { _customerName = value; OnPropertyChanged(); }
        }

        private string _staffName = string.Empty;
        public string StaffName
        {
            get => _staffName;
            set { _staffName = value; OnPropertyChanged(); }
        }

        private DateTime _createdAt;
        public DateTime CreatedAt
        {
            get => _createdAt;
            set { _createdAt = value; OnPropertyChanged(); }
        }

        private decimal _totalPrice;
        public decimal TotalPrice
        {
            get => _totalPrice;
            set { _totalPrice = value; OnPropertyChanged(); }
        }

        private int _pointUsed;
        public int PointUsed
        {
            get => _pointUsed;
            set { _pointUsed = value; OnPropertyChanged(); }
        }

        private int _pointReceived;
        public int PointReceived
        {
            get => _pointReceived;
            set { _pointReceived = value; OnPropertyChanged(); }
        }

        public ObservableCollection<OrderDetailEditItem> OrderItems
        {
            get => _orderItems;
            set { _orderItems = value; OnPropertyChanged(); RecalculateTotal(); }
        }

        public ICommand IncreaseQtyCommand { get; }
        public ICommand DecreaseQtyCommand { get; }
        public ICommand RemoveItemCommand { get; }
        public ICommand UpdateCommand { get; }
        public ICommand GoBackCommand { get; }
        public ICommand ExportPdfCommand { get; }

        // Event để View xử lý navigation
        public event EventHandler? RequestNavigationBack;

        public OrderDetailViewModel(int orderId)
        {
            _orderId = orderId;
            OrderId = orderId;

            IncreaseQtyCommand = new RelayCommand(IncreaseQty);
            DecreaseQtyCommand = new RelayCommand(DecreaseQty);
            RemoveItemCommand = new RelayCommand(RemoveItem);
            UpdateCommand = new RelayCommand(UpdateOrder);
            GoBackCommand = new RelayCommand(GoBack);
            ExportPdfCommand = new RelayCommand(ExportPdf);

            LoadOrderDetail(orderId);
        }

        private void LoadOrderDetail(int orderId)
        {
            using var db = new Prn212G3Context();
            var order = db.Orders
                .Include(o => o.Customer)
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefault(o => o.OrderId == orderId);

            if (order == null) return;

            CustomerName = order.Customer?.CustomerName ?? "";
            StaffName = order.User?.UserName ?? "";
            CreatedAt = order.CreatedAt ?? DateTime.Now;
            PointUsed = order.PointUsed ?? 0;
            PointReceived = order.PointReceived ?? 0;

            OrderItems = new ObservableCollection<OrderDetailEditItem>(
                order.OrderDetails.Select(od => new OrderDetailEditItem
                {
                    OrderDetailId = od.OrderDetailId,
                    ProductId = od.ProductId,
                    ProductName = od.Product.ProductName,
                    UnitPrice = od.Product.Price ?? 0,
                    Quantity = od.Quantity ?? 0
                }));
        }

        private void IncreaseQty(object obj)
        {
            if (obj is OrderDetailEditItem item)
            {
                item.Quantity++;
                RecalculateTotal();
            }
        }

        private void DecreaseQty(object obj)
        {
            if (obj is OrderDetailEditItem item && item.Quantity > 1)
            {
                item.Quantity--;
                RecalculateTotal();
            }
        }

        private void RemoveItem(object obj)
        {
            if (obj is OrderDetailEditItem item)
            {
                if (OrderItems.Count <= 1)
                {
                    MessageBox.Show("An order must contain at least 1 product.");
                    return;
                }

                _removedDetailIds.Add(item.OrderDetailId);
                OrderItems.Remove(item);
                RecalculateTotal();
            }
        }

        private void RecalculateTotal()
        {
            TotalPrice = OrderItems.Sum(i => i.Total);
        }

        private void UpdateOrder(object obj)
        {
            using var db = new Prn212G3Context();
            using var tx = db.Database.BeginTransaction();
            try
            {
                var order = db.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefault(o => o.OrderId == _orderId);

                if (order == null)
                {
                    MessageBox.Show("Order not found");
                    return;
                }

                // Must keep at least 1 item
                if (OrderItems.Count == 0 || (OrderItems.Count == 1 && _removedDetailIds.Contains(OrderItems[0].OrderDetailId)))
                {
                    MessageBox.Show("Cannot update: an order must contain at least 1 product.");
                    return;
                }

                // Remove deleted details
                if (_removedDetailIds.Count > 0)
                {
                    var toRemove = order.OrderDetails.Where(d => _removedDetailIds.Contains(d.OrderDetailId)).ToList();
                    if (toRemove.Count > 0)
                    {
                        db.OrderDetails.RemoveRange(toRemove);
                    }
                }

                // Update detail quantities
                foreach (var item in OrderItems)
                {
                    var detail = order.OrderDetails.FirstOrDefault(d => d.OrderDetailId == item.OrderDetailId);
                    if (detail != null)
                    {
                        detail.Quantity = item.Quantity;
                    }
                }

                // Recalculate total from current product prices
                var detailWithProducts = db.OrderDetails
                    .Include(d => d.Product)
                    .Where(d => d.OrderId == _orderId)
                    .ToList();

                var newTotal = detailWithProducts.Sum(d => (d.Product.Price ?? 0) * (d.Quantity ?? 0));
                order.TotalPrice = newTotal;

                // Recalculate points based on business rules
                if ((order.PointUsed ?? 0) > 0)
                {
                    // Max 10% of order value, 1 point = 1₫ when using points
                    var maxDiscount = (int)(newTotal / 10m);
                    var currentUsed = order.PointUsed ?? 0;
                    order.PointUsed = currentUsed > maxDiscount ? maxDiscount : currentUsed;
                    order.PointReceived = 0;
                    PointUsed = order.PointUsed ?? 0;
                    PointReceived = 0;
                }
                else
                {
                    // Not using points: 100₫ = 1 point
                    var received = (int)(newTotal / 100m);
                    order.PointReceived = received;
                    PointReceived = received;
                }

                db.SaveChanges();
                tx.Commit();

                MessageBox.Show("Order updated successfully.");
                _removedDetailIds.Clear();
                RecalculateTotal();
            }
            catch (Exception ex)
            {
                tx.Rollback();
                MessageBox.Show($"Error: {ex.Message}");
            }
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
        private void ExportPdf(object obj)
        {
            try
            {
                string fileName = $"Invoice_{OrderId}.pdf";
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string filePath = Path.Combine(desktopPath, fileName);

                PdfDocument document = new PdfDocument();
                document.Info.Title = "Invoice";

                PdfPage page = document.AddPage();
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XFont headerFont = new XFont("Arial", 18, XFontStyle.Bold);
                XFont textFont = new XFont("Arial", 12);
                XFont boldFont = new XFont("Arial", 12, XFontStyle.Bold);

                double y = 40;

                gfx.DrawString("INVOICE", headerFont, XBrushes.Black,
                    new XRect(0, y, page.Width, 30), XStringFormats.TopCenter);
                y += 50;

                gfx.DrawString($"Order ID: {OrderId}", textFont, XBrushes.Black, 40, y);
                y += 20;
                gfx.DrawString($"Customer: {CustomerName}", textFont, XBrushes.Black, 40, y);
                y += 20;
                gfx.DrawString($"Staff: {StaffName}", textFont, XBrushes.Black, 40, y);
                y += 20;
                gfx.DrawString($"Created At: {CreatedAt:dd/MM/yyyy HH:mm}", textFont, XBrushes.Black, 40, y);
                y += 30;

                gfx.DrawLine(XPens.Black, 40, y, page.Width - 40, y);
                y += 15;

                // Header Table
                gfx.DrawString("Product", boldFont, XBrushes.Black, 40, y);
                gfx.DrawString("Qty", boldFont, XBrushes.Black, 270, y);
                gfx.DrawString("Unit Price", boldFont, XBrushes.Black, 330, y);
                gfx.DrawString("Total", boldFont, XBrushes.Black, 450, y);
                y += 10;
                gfx.DrawLine(XPens.Black, 40, y, page.Width - 40, y);
                y += 20;

                foreach (var item in OrderItems)
                {
                    gfx.DrawString(item.ProductName, textFont, XBrushes.Black, 40, y);
                    gfx.DrawString(item.Quantity.ToString(), textFont, XBrushes.Black, 270, y);
                    gfx.DrawString($"{item.UnitPrice:N0} ₫", textFont, XBrushes.Black, 330, y);
                    gfx.DrawString($"{item.Total:N0} ₫", textFont, XBrushes.Black, 450, y);
                    y += 20;

                    // Xuống dòng trang nếu quá dài
                    if (y > page.Height - 100)
                    {
                        page = document.AddPage();
                        gfx = XGraphics.FromPdfPage(page);
                        y = 40;
                    }
                }

                y += 10;
                gfx.DrawLine(XPens.Black, 40, y, page.Width - 40, y);
                y += 25;

                gfx.DrawString($"Total Price: {TotalPrice:N0} ₫", boldFont, XBrushes.Black, 380, y);
                y += 20;
                gfx.DrawString($"Points Used: {PointUsed}", textFont, XBrushes.Black, 380, y);
                y += 20;
                gfx.DrawString($"Points Received: {PointReceived}", textFont, XBrushes.Green, 380, y);

                document.Save(filePath);

                MessageBox.Show($"PDF exported successfully:\n{filePath}", "Export PDF",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export PDF failed:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class OrderDetailEditItem : INotifyPropertyChanged
    {
        public int OrderDetailId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(); OnPropertyChanged(nameof(Total)); }
        }

        public decimal Total => UnitPrice * Quantity;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using SMA.Models;
using Microsoft.EntityFrameworkCore;

namespace SMA.Services
{
    /// <summary>
    /// Stock Monitor Service - Subject trong Observer Pattern
    /// </summary>
    public class StockMonitor
    {
        private static StockMonitor? _instance;
        private readonly List<IStockObserver> _observers = new();
        private const int LOW_STOCK_THRESHOLD = 10; 
        private HashSet<int> _notifiedProductIds = new();
        private DateTime _lastNotificationTime = DateTime.MinValue;
        private const int NOTIFICATION_COOLDOWN_SECONDS = 5;

        private StockMonitor() { }

        /// <summary>
        /// Singleton instance`
        /// </summary>
        public static StockMonitor Instance
        {
            get
            {
                _instance ??= new StockMonitor();
                return _instance;
            }
        }

        /// <summary>
        /// Đăng ký observer
        /// </summary>
        public void Subscribe(IStockObserver observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }
        }

        /// <summary>
        /// Hủy đăng ký observer
        /// </summary>
        public void Unsubscribe(IStockObserver observer)
        {
            _observers.Remove(observer);
        }

        /// <summary>
        /// Kiểm tra và thông báo các sản phẩm có stock thấp
        /// </summary>
        public void CheckLowStock(Prn212G3Context dbContext)
        {
            var lowStockProducts = dbContext.Products
                .Where(p => p.StockQuantity < LOW_STOCK_THRESHOLD && (p.IsActive == true || p.IsActive == null))
                .ToList();

            if (lowStockProducts.Any())
            {
                // cooldown
                var timeSinceLastNotification = (DateTime.Now - _lastNotificationTime).TotalSeconds;
                if (timeSinceLastNotification < NOTIFICATION_COOLDOWN_SECONDS)
                {
                    return;
                }

                var currentProductIds = new HashSet<int>(lowStockProducts.Select(p => p.ProductId));
                
                var newProducts = lowStockProducts.Where(p => !_notifiedProductIds.Contains(p.ProductId)).ToList();
                
                if (newProducts.Any())
                {
                    _notifiedProductIds = currentProductIds;
                    _lastNotificationTime = DateTime.Now;
                    
                    NotifyObservers(lowStockProducts);
                }
            }
            else
            {
                _notifiedProductIds.Clear();
            }
        }

        /// <summary>
        /// Reset danh sách sản phẩm đã được cảnh báo (khi user chọn No hoặc đóng window)
        /// </summary>
        public void ResetNotifiedProducts()
        {
            _notifiedProductIds.Clear();
        }

        /// <summary>
        /// Observer action
        /// </summary>
        private void NotifyObservers(List<Product> lowStockProducts)
        {
            foreach (var observer in _observers.ToList())
            {
                try
                {
                    observer.OnLowStockDetected(lowStockProducts);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error notifying observer: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Lấy ngưỡng cảnh báo
        /// </summary>
        public int GetLowStockThreshold() => LOW_STOCK_THRESHOLD;
    }
}

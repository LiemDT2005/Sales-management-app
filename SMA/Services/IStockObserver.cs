using SMA.Models;

namespace SMA.Services
{
    /// <summary>
    /// Observer interface cho Stock Monitoring
    /// </summary>
    public interface IStockObserver
    {
        /// <summary>
        /// Được gọi khi phát hiện sản phẩm <10
        /// </summary>
        void OnLowStockDetected(List<Product> lowStockProducts);
    }
}

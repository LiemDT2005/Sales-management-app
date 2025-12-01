using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace SMA.Converters
{
    public class ImagePathConverter : IValueConverter
    {
        private const string DefaultFolder = "Assets/Products";
        private const string DefaultFileName = "default.png"; // đặt ảnh mặc định ở Assets/Products/default.png

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var fileName = value?.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    var path = TryResolvePath(fileName);
                    if (path != null && File.Exists(path))
                        return LoadImageFromFile(path);
                }

                var defaultPath = TryResolvePath(DefaultFileName);
                if (defaultPath != null && File.Exists(defaultPath))
                    return LoadImageFromFile(defaultPath);

                // 3) Fallback: nếu bạn đã embed default.png (Build Action: Resource)
                // thì dùng pack URI
                var packUri = $"pack://application:,,,/{DefaultFolder}/{DefaultFileName}";
                var img = LoadImageFromUri(new Uri(packUri, UriKind.Absolute));
                if (img != null) return img;

                // 4) Bất khả dụng -> yêu cầu WPF bỏ qua binding
                return DependencyProperty.UnsetValue;
            }
            catch
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;

        // ===== Helpers =====
        private static string TryResolvePath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return null;

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // Output: ...\bin\Debug\netX\Assets\Products\fileName
            var p1 = Path.Combine(baseDir, DefaultFolder, fileName);
            if (File.Exists(p1)) return Path.GetFullPath(p1);

            // Dev mode: ...\projectRoot\Assets\Products\fileName
            // (đi ngược 2 cấp: bin\netX)
            var projectRoot = Directory.GetParent(baseDir)?.Parent?.Parent?.FullName;
            if (!string.IsNullOrEmpty(projectRoot))
            {
                var p2 = Path.Combine(projectRoot, DefaultFolder, fileName);
                if (File.Exists(p2)) return Path.GetFullPath(p2);
            }

            return null;
        }

        private static BitmapImage LoadImageFromFile(string fullPath)
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(fullPath, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;              // load ngay để unlock file
                bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache; // tránh cache cũ
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch
            {
                return null;
            }
        }

        private static BitmapImage LoadImageFromUri(Uri uri)
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = uri;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch
            {
                return null;
            }
        }
    }
}

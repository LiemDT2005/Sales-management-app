using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace SMA.Helper
{
    /// <summary>
    /// Cho phép animate (trượt mượt) thuộc tính GridLength, ví dụ như ColumnDefinition.Width hoặc RowDefinition.Height.
    /// Dùng cho sidebar thu/mở, thanh panel co giãn mượt trong WPF.
    /// </summary>
    public class GridLengthAnimation : AnimationTimeline
    {
        // Xác định kiểu property mà animation này tác động (GridLength)
        public override Type TargetPropertyType => typeof(GridLength);

        // Thuộc tính From (giá trị bắt đầu)
        public GridLength From
        {
            get => (GridLength)GetValue(FromProperty);
            set => SetValue(FromProperty, value);
        }
        public static readonly DependencyProperty FromProperty =
            DependencyProperty.Register(nameof(From), typeof(GridLength), typeof(GridLengthAnimation));

        // Thuộc tính To (giá trị kết thúc)
        public GridLength To
        {
            get => (GridLength)GetValue(ToProperty);
            set => SetValue(ToProperty, value);
        }
        public static readonly DependencyProperty ToProperty =
            DependencyProperty.Register(nameof(To), typeof(GridLength), typeof(GridLengthAnimation));

        // Hàm chính để tính giá trị hiện tại theo thời gian
        public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
        {
            // Lấy giá trị From/To (đơn vị pixel)
            double fromValue = ((GridLength)GetValue(FromProperty)).Value;
            double toValue = ((GridLength)GetValue(ToProperty)).Value;

            // Lấy tiến trình animation (0 → 1)
            double progress = animationClock.CurrentProgress ?? 0;

            // Tính toán giá trị trung gian
            double currentValue = fromValue + (toValue - fromValue) * progress;

            // Trả về GridLength mới (pixel)
            return new GridLength(currentValue, GridUnitType.Pixel);
        }

        // Bắt buộc override để WPF có thể clone object
        protected override Freezable CreateInstanceCore() => new GridLengthAnimation();
    }
}

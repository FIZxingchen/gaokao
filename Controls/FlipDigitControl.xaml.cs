using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace gokao
{
    public partial class FlipDigitControl : UserControl
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(int), typeof(FlipDigitControl),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        private int _currentValue;
        private bool _isFlipping;
        private bool _isLoaded;

        private RenderTargetBitmap[] _digitImages = new RenderTargetBitmap[10];
        private double _digitFontSize = 72;
        private Color _digitTextColor = Colors.White;
        private string _digitFontFamily = "Arial, Microsoft YaHei";

        // 上次应用的样式缓存：拖动滑块高频调用时，样式未变则跳过重建位图
        private bool _styleApplied;
        private double _lastFontSize = -1;
        private Color _lastTextColor;
        private string _lastFontFamily;

        /// <summary>翻页数字控件基准宽度（字号 72 时）</summary>
        private const double BaseControlWidth = 40;
        /// <summary>翻页数字控件基准高度（字号 72 时）</summary>
        private const double BaseControlHeight = 60;
        /// <summary>渲染位图 = 控件尺寸 × 1.5（平衡清晰度与内存占用）</summary>
        private const double RenderScale = 1.5;

        private double _renderWidth = 80;
        private double _renderHeight = 120;

        public FlipDigitControl()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                EnsureImages();
                UpdateFace(_currentValue, false);
                rotation.Angle = 0;
                _isLoaded = true;
            };
        }

        private void EnsureImages()
        {
            for (int i = 0; i < 10; i++)
                _digitImages[i] = CreateDigitImage(i);
        }

        /// <summary>
        /// 释放全部数字位图：非翻页模式下调用以回收内存。
        /// 重置 _styleApplied 使下次 ApplyStyle 必然重建。
        /// </summary>
        public void ReleaseImages()
        {
            for (int i = 0; i < _digitImages.Length; i++)
                _digitImages[i] = null;
            _styleApplied = false;
        }

        public void ApplyStyle(double fontSize, Color textColor, string fontFamily = "Arial, Microsoft YaHei")
        {
            // 样式未变化时直接跳过，避免拖动滑块时反复重建 10 张位图
            if (_styleApplied &&
                Math.Abs(_lastFontSize - fontSize) < 0.01 &&
                _lastTextColor == textColor &&
                _lastFontFamily == fontFamily)
                return;

            _styleApplied = true;
            _lastFontSize = fontSize;
            _lastTextColor = textColor;
            _lastFontFamily = fontFamily;

            _digitFontSize = fontSize;
            _digitTextColor = textColor;
            _digitFontFamily = fontFamily;

            // 根据字号缩放控件物理尺寸
            double scale = fontSize / 72.0;
            double ctrlW = Math.Max(20, BaseControlWidth * scale);
            double ctrlH = Math.Max(30, BaseControlHeight * scale);
            Width = ctrlW;
            Height = ctrlH;
            viewport3D.Width = ctrlW;
            viewport3D.Height = ctrlH;
            _renderWidth = ctrlW * RenderScale;
            _renderHeight = ctrlH * RenderScale;

            EnsureImages();
            if (_isLoaded)
            {
                // 同时更新正面和背面纹理，确保无论是否翻页都立即显示新颜色
                UpdateFace(_currentValue, false);
                UpdateFace(_currentValue, true);
                rotation.Angle = 0;
            }
        }

        private RenderTargetBitmap CreateDigitImage(int digit)
        {
            double rw = _renderWidth;
            double rh = _renderHeight;
            var bitmap = new RenderTargetBitmap(
                (int)rw, (int)rh, 96, 96, PixelFormats.Pbgra32);

            // 卡片容器：渐变背景 + 圆角 + 微光边框
            double cornerRadius = Math.Min(rw, rh) * 0.12;
            var border = new Border
            {
                Width = rw,
                Height = rh,
                CornerRadius = new CornerRadius(cornerRadius),
                BorderThickness = new Thickness(1.5),
                BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255))
            };

            // 渐变：左上偏亮 → 右下偏暗，营造卡片立体感
            var gradient = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0.2, 1)
            };
            gradient.GradientStops.Add(new GradientStop(Color.FromArgb(195, 40, 40, 76), 0));
            gradient.GradientStops.Add(new GradientStop(Color.FromArgb(150, 18, 18, 38), 1));
            border.Background = gradient;

            // 内层 Grid：中央铰链线 + 数字文字
            var grid = new Grid();

            // 中央铰链线（翻页时钟风格，极淡的暗色分割线）
            var separator = new Border
            {
                Height = 1.5,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = new SolidColorBrush(Color.FromArgb(35, 0, 0, 0))
            };
            grid.Children.Add(separator);

            // 数字文字 + 柔光发光效果
            var textBlock = new TextBlock
            {
                Text = digit.ToString(),
                FontSize = _digitFontSize,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(_digitTextColor),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = new FontFamily(_digitFontFamily)
            };
            textBlock.Effect = new DropShadowEffect
            {
                Color = _digitTextColor,
                BlurRadius = _digitFontSize * 0.12,
                ShadowDepth = 0,
                Opacity = 0.35
            };
            grid.Children.Add(textBlock);

            border.Child = grid;

            border.Measure(new Size(rw, rh));
            border.Arrange(new Rect(0, 0, rw, rh));
            bitmap.Render(border);
            bitmap.Freeze();
            return bitmap;
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FlipDigitControl control = d as FlipDigitControl;
            if (control == null) return;
            int newVal = (int)e.NewValue;
            if (newVal < 0) newVal = 0;
            if (newVal > 9) newVal = 9;

            if (!control._isLoaded)
            {
                control._currentValue = newVal;
                control.UpdateFace(newVal, false);
                control.rotation.Angle = 0;
            }
            else
            {
                control.FlipTo(newVal);
            }
        }

        private void FlipTo(int newValue)
        {
            if (_isFlipping || newValue == _currentValue) return;
            UpdateFace(newValue, true);

            _isFlipping = true;
            var anim = new DoubleAnimation
            {
                From = 0, To = 180,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            anim.Completed += (sender, args) =>
            {
                _currentValue = newValue;
                _isFlipping = false;
                UpdateFace(newValue, false);
                rotation.Angle = 0;
            };
            rotation.BeginAnimation(AxisAngleRotation3D.AngleProperty, anim);
        }

        private void UpdateFace(int digit, bool isBack)
        {
            DiffuseMaterial material = isBack ? backMaterial : frontMaterial;
            RenderTargetBitmap image = _digitImages[digit];
            if (image == null) return;
            material.Brush = new ImageBrush(image)
            {
                Stretch = Stretch.Fill,
                ViewportUnits = BrushMappingMode.Absolute,
                TileMode = TileMode.None
            };
        }
    }
}

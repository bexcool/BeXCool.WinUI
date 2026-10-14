using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace BeXCool.WinUI.Controls
{
    [ContentProperty(Name = nameof(Content))]
    public class Badge : ContentControl
    {
        public Brush BadgeBrush
        {
            get => (Brush)GetValue(BadgeBrushProperty);
            set => SetValue(BadgeBrushProperty, value);
        }

        public static readonly DependencyProperty BadgeBrushProperty =
            DependencyProperty.Register(nameof(BadgeBrush),
            typeof(Brush),
            typeof(Badge),
            new PropertyMetadata(new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 180, 0)), OnBadgeBrushChangedCallback));

        private static void OnBadgeBrushChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (Badge)d;
        }

#nullable enable
        private Grid? _rootGrid;
        private Border? _fillLayer;
        private Border? _outlineLayer;
#nullable disable

        public Badge()
        {
            DefaultStyleKey = typeof(Badge);
        }

        protected override void OnApplyTemplate()
        {
            if (_rootGrid != null)
                _rootGrid.SizeChanged -= OnRootSizeChanged;

            base.OnApplyTemplate();

            _rootGrid = GetTemplateChild("RootGrid") as Grid;
            _fillLayer = GetTemplateChild("FillLayer") as Border;
            _outlineLayer = GetTemplateChild("OutlineLayer") as Border;

            if (_rootGrid != null)
                _rootGrid.SizeChanged += OnRootSizeChanged;
        }

        /// <summary>
        /// Keeps ends fully rounded (pill shape) for any content height
        /// </summary>
        private void OnRootSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var radius = new CornerRadius(e.NewSize.Height / 2);
            _fillLayer?.CornerRadius = radius;
            _outlineLayer?.CornerRadius = radius;
        }
    }
}

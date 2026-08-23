using BeXCool.Toolkit.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace BeXCool.WinUI.Controls
{
    public sealed partial class ReturnDefaultButton : Button
    {
        public string BoundPropertyName
        {
            get => (string)GetValue(BoundPropertyNameProperty);
            set => SetValue(BoundPropertyNameProperty, value);
        }

        public static readonly DependencyProperty BoundPropertyNameProperty =
            DependencyProperty.Register(
                nameof(BoundPropertyName),
                typeof(string),
                typeof(ReturnDefaultButton),
                new PropertyMetadata(null, OnBoundPropertyChanged));

        public object DefaultBoundPropetryValue { get; private set; }

        private INotifyPropertyChanged _subscribedObject;
        private string _subscribedLeafPropertyName;

        public ReturnDefaultButton()
        {
            InitializeComponent();

            Click += Button_Click;
            Loaded += (s, e) => RefreshBindings();
            Unloaded += (s, e) => UnsubscribeFromTarget();
            DataContextChanged += (s, e) => RefreshBindings();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var (obj, propInfo) = GetPropertyInfo(DataContext, BoundPropertyName);

            if (obj != null && propInfo != null)
            {
                propInfo.SetValue(obj, DefaultBoundPropetryValue);
                UpdateVisibility();
            }
        }

        private static void OnBoundPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ReturnDefaultButton button)
            {
                button.RefreshBindings();
            }
        }

        private void RefreshBindings()
        {
            UnsubscribeFromTarget();

            if (DataContext == null || string.IsNullOrWhiteSpace(BoundPropertyName))
            {
                Visibility = Visibility.Collapsed;
                return;
            }

            var (obj, propInfo) = GetPropertyInfo(DataContext, BoundPropertyName);
            if (obj is INotifyPropertyChanged inpc && propInfo != null)
            {
                _subscribedObject = inpc;
                _subscribedLeafPropertyName = propInfo.Name;
                _subscribedObject.PropertyChanged += OnTargetPropertyChanged;
            }

            UpdateVisibility();
        }

        private void OnTargetPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == _subscribedLeafPropertyName)
            {
                UpdateVisibility();
            }
        }

        private void UnsubscribeFromTarget()
        {
            if (_subscribedObject != null)
            {
                _subscribedObject.PropertyChanged -= OnTargetPropertyChanged;
                _subscribedObject = null;
                _subscribedLeafPropertyName = null;
            }
        }

        private void UpdateVisibility()
        {
            if (DataContext == null || string.IsNullOrWhiteSpace(BoundPropertyName))
            {
                Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                var (obj, propInfo) = GetPropertyInfo(DataContext, BoundPropertyName);
                if (obj == null || propInfo == null)
                {
                    Visibility = Visibility.Collapsed;
                    return;
                }

                var currentValue = propInfo.GetValue(obj);
                DefaultBoundPropetryValue = obj.GetDefaultValue(BoundPropertyName.Split('.').Last());

                var isChanged = !Equals(currentValue, DefaultBoundPropetryValue);
                Visibility = isChanged ? Visibility.Visible : Visibility.Collapsed;
            }
            catch
            {
                Visibility = Visibility.Collapsed;
            }
        }

        private (object obj, PropertyInfo propInfo) GetPropertyInfo(object obj, string path)
        {
            var parts = path.Split('.');
            var current = obj;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                var prop = current?.GetType().GetProperty(parts[i]);
                if (prop == null)
                    return (null, null);

                current = prop.GetValue(current);
            }

            var finalProp = current?.GetType().GetProperty(parts[^1]);
            return (current, finalProp);
        }
    }
}
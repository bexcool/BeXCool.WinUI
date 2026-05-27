using BeXCool.Toolkit.Reflection.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

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

        public ReturnDefaultButton()
        {
            InitializeComponent();
            this.Loaded += (s, e) => UpdateVisibility();
            this.DataContextChanged += (s, e) => UpdateVisibility();
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
                button.UpdateVisibility();
            }
        }

        private void UpdateVisibility()
        {
            if (DataContext == null || string.IsNullOrEmpty(BoundPropertyName))
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

        private (object, PropertyInfo) GetPropertyInfo(object obj, string path)
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

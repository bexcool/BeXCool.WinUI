using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;

namespace BeXCool.WinUI.Controls
{
    /// <summary>
    /// <see cref="WhatsNewView"/> in a dialog with a header and a close button. Set <see cref="ContentDialog.XamlRoot"/> before
    /// calling <see cref="ContentDialog.ShowAsync"/>. <see cref="View"/> gives access to everything else of the view.
    /// <see cref="WhatsNewController.ShowDialogAsync"/> creates and shows it from <see cref="WhatsNewOptions"/>.
    /// </summary>
    public sealed partial class WhatsNewDialog : ContentDialog
    {
        // Space the dialog keeps around the content when the window is too small for the preferred size
        private const double WindowMarginWidth = 96;
        private const double WindowMarginHeight = 120;

        public WhatsNewView View => ContentView;

        public IList<WhatsNewItem> Items => ContentView.Items;

        public object ItemsSource
        {
            get => ContentView.ItemsSource;
            set => ContentView.ItemsSource = value;
        }

        public int SelectedIndex
        {
            get => ContentView.SelectedIndex;
            set => ContentView.SelectedIndex = value;
        }

        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(nameof(Header), typeof(string), typeof(WhatsNewDialog), new PropertyMetadata("What's new"));

        public string CloseButtonToolTip
        {
            get => (string)GetValue(CloseButtonToolTipProperty);
            set => SetValue(CloseButtonToolTipProperty, value);
        }

        public static readonly DependencyProperty CloseButtonToolTipProperty =
            DependencyProperty.Register(nameof(CloseButtonToolTip), typeof(string), typeof(WhatsNewDialog), new PropertyMetadata("Close"));

        /// <summary>
        /// Hide the dialog when the action button of a card is clicked. True by default.
        /// </summary>
        public bool CloseOnAction { get; set; } = true;

        /// <summary>
        /// Size of the content (header and view) when the window is large enough.
        /// </summary>
        public double PreferredContentWidth { get; set; } = 760;
        public double PreferredContentHeight { get; set; } = 460;

        public WhatsNewDialog()
        {
            // The implicit ContentDialog style doesn't apply to a derived type
            try
            {
                if (Application.Current.Resources["DefaultContentDialogStyle"] is Style style)
                    Style = style;
            }
            catch (Exception) { }

            InitializeComponent();

            // Loaded comes before the opening animation, so the dialog never shows up in another size first
            Loaded += (s, e) =>
            {
                XamlRoot.Changed += XamlRoot_Changed;
                UpdateContentSize();
            };
            Closed += (s, e) => XamlRoot.Changed -= XamlRoot_Changed;
        }

        private void XamlRoot_Changed(XamlRoot sender, XamlRootChangedEventArgs args) => UpdateContentSize();

        private void UpdateContentSize()
        {
            if (XamlRoot is null)
                return;

            RootGrid.Width = Math.Max(320, Math.Min(PreferredContentWidth, XamlRoot.Size.Width - WindowMarginWidth));
            RootGrid.Height = Math.Max(240, Math.Min(PreferredContentHeight, XamlRoot.Size.Height - WindowMarginHeight));
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Hide();

        private void ContentView_ActionInvoked(WhatsNewView sender, WhatsNewItemEventArgs args)
        {
            if (CloseOnAction)
                Hide();
        }
    }
}

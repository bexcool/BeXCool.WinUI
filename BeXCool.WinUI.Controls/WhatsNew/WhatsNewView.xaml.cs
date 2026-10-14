using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;

namespace BeXCool.WinUI.Controls
{
    /// <summary>
    /// "What's new" overview: a list of <see cref="WhatsNewItem"/>s on the left and a card with the selected one on the right,
    /// which slides to the next card when the selection changes. Fill <see cref="Items"/> (also directly in XAML) or bind
    /// <see cref="ItemsSource"/>. To show it in a dialog, use <see cref="WhatsNewDialog"/>.
    /// </summary>
    [ContentProperty(Name = nameof(Items))]
    public sealed partial class WhatsNewView : UserControl
    {
        private const double CardSpacing = 16;
        private static readonly TimeSpan TransitionDuration = TimeSpan.FromMilliseconds(450);

        private static readonly UISettings _uiSettings = new();

        // The card on screen, and while sliding also the one leaving it
        private ContentControl _currentCard;
        private ContentControl _leavingCard;
        private Storyboard _transition;
        private int _shownIndex = -1;

        private bool _syncingSelection;

        public event TypedEventHandler<WhatsNewView, WhatsNewItemEventArgs> SelectionChanged;

        /// <summary>
        /// Raised when the action button of a card is clicked, before its <see cref="WhatsNewItem.ActionCommand"/> runs.
        /// </summary>
        public event TypedEventHandler<WhatsNewView, WhatsNewItemEventArgs> ActionInvoked;

        /// <summary>
        /// Items shown when <see cref="ItemsSource"/> is not set.
        /// </summary>
        public IList<WhatsNewItem> Items { get; } = new ObservableCollection<WhatsNewItem>();

        /// <summary>
        /// Collection of <see cref="WhatsNewItem"/>s used instead of <see cref="Items"/>.
        /// </summary>
        public object ItemsSource
        {
            get => GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(object), typeof(WhatsNewView),
                new PropertyMetadata(null, (d, e) => ((WhatsNewView)d).UpdateItemsSource()));

        public int SelectedIndex
        {
            get => (int)GetValue(SelectedIndexProperty);
            set => SetValue(SelectedIndexProperty, value);
        }

        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(WhatsNewView),
                new PropertyMetadata(0, (d, e) => ((WhatsNewView)d).ApplySelectedIndex()));

        public WhatsNewItem SelectedItem => ItemsListView.SelectedItem as WhatsNewItem;

        /// <summary>
        /// Width of the list on the left.
        /// </summary>
        public double PaneWidth
        {
            get => (double)GetValue(PaneWidthProperty);
            set => SetValue(PaneWidthProperty, value);
        }

        public static readonly DependencyProperty PaneWidthProperty =
            DependencyProperty.Register(nameof(PaneWidth), typeof(double), typeof(WhatsNewView), new PropertyMetadata(240d));

        /// <summary>
        /// Template of the list items, with a <see cref="WhatsNewItem"/> as data. Null uses the default one.
        /// </summary>
        public DataTemplate ItemTemplate
        {
            get => (DataTemplate)GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(WhatsNewView),
                new PropertyMetadata(null, (d, e) => ((WhatsNewView)d).UpdateItemTemplate()));

        /// <summary>
        /// Template of the card, with a <see cref="WhatsNewItem"/> as data. Null uses the default one.
        /// </summary>
        public DataTemplate CardTemplate
        {
            get => (DataTemplate)GetValue(CardTemplateProperty);
            set => SetValue(CardTemplateProperty, value);
        }

        public static readonly DependencyProperty CardTemplateProperty =
            DependencyProperty.Register(nameof(CardTemplate), typeof(DataTemplate), typeof(WhatsNewView),
                new PropertyMetadata(null, (d, e) => ((WhatsNewView)d).RecreateCurrentCard()));

        public WhatsNewView()
        {
            InitializeComponent();

            ItemsListView.Items.VectorChanged += ListItems_VectorChanged;
            UpdateItemsSource();
        }

        #region Template helpers

        public static Visibility TextVisibility(string text)
            => string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;

        public static Visibility HeroVisibility(ImageSource imageSource, string glyph)
            => imageSource is null && string.IsNullOrEmpty(glyph) ? Visibility.Collapsed : Visibility.Visible;

        public static string FallbackText(string text, string fallback)
            => string.IsNullOrEmpty(text) ? fallback : text;

        #endregion

        #region Items and selection

        private void UpdateItemsSource()
        {
            ItemsListView.ItemsSource = ItemsSource ?? Items;
        }

        private void UpdateItemTemplate()
        {
            ItemsListView.ItemTemplate = ItemTemplate ?? (DataTemplate)Resources["DefaultListItemTemplate"];
        }

        private void ListItems_VectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs e)
        {
            // The list is still in the middle of the change here, so select after it. Items added one by one from XAML
            // queue this several times, which doesn't matter, every call ends in the same state.
            DispatcherQueue.TryEnqueue(() =>
            {
                // Keep something selected - the list has no selection after a reset or when the selected item is removed
                if (ItemsListView.SelectedIndex < 0)
                    ApplySelectedIndex();
                else
                    ShowSelectedItem();
            });
        }

        private void ApplySelectedIndex()
        {
            int count = ItemsListView.Items.Count;
            int index = count == 0 ? -1 : Math.Clamp(SelectedIndex, 0, count - 1);

            if (ItemsListView.SelectedIndex != index)
                ItemsListView.SelectedIndex = index;
            else
                ShowSelectedItem();
        }

        private void ItemsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Ctrl+click deselects even in single selection mode, but some card is always shown
            if (ItemsListView.SelectedIndex < 0 && ItemsListView.Items.Count > 0)
            {
                ItemsListView.SelectedIndex = Math.Clamp(_shownIndex, 0, ItemsListView.Items.Count - 1);
                return;
            }

            if (!_syncingSelection && ItemsListView.SelectedIndex >= 0 && SelectedIndex != ItemsListView.SelectedIndex)
            {
                _syncingSelection = true;
                SelectedIndex = ItemsListView.SelectedIndex;
                _syncingSelection = false;
            }

            ShowSelectedItem();
        }

        #endregion

        #region Cards

        private void ShowSelectedItem()
        {
            var item = SelectedItem;
            int index = ItemsListView.SelectedIndex;

            if (ReferenceEquals(_currentCard?.Content, item))
            {
                _shownIndex = index;
                return;
            }

            int direction = _shownIndex < 0 || index < 0 ? 0 : Math.Sign(index - _shownIndex);
            _shownIndex = index;

            ShowCard(item, direction);

            SelectionChanged?.Invoke(this, new WhatsNewItemEventArgs(item));
        }

        /// <param name="direction">1 slides the new card in from the right, -1 from the left, 0 swaps it without animation.</param>
        private void ShowCard(WhatsNewItem item, int direction)
        {
            // A quick second change lands the running transition first
            FinishTransition();

            var previous = _currentCard;
            _currentCard = item is null ? null : CreateCard(item);

            if (_currentCard is not null)
                CardHost.Children.Add(_currentCard);

            if (previous is null)
                return;

            if (direction == 0 || _currentCard is null || !IsLoaded || CardHost.ActualWidth <= 0 || !_uiSettings.AnimationsEnabled)
            {
                CardHost.Children.Remove(previous);
                return;
            }

            // The leaving card can't be clicked any more, it's on its way out
            previous.IsHitTestVisible = false;
            _leavingCard = previous;

            double distance = CardHost.ActualWidth + CardSpacing;

            var transition = new Storyboard();
            transition.Children.Add(CreateSlideAnimation(previous, 0, -direction * distance));
            transition.Children.Add(CreateSlideAnimation(_currentCard, direction * distance, 0));
            transition.Completed += (s, e) =>
            {
                if (_transition == transition)
                    FinishTransition();
            };

            _transition = transition;
            transition.Begin();
        }

        private void FinishTransition()
        {
            if (_transition is not null)
            {
                var transition = _transition;
                _transition = null;

                // Stop returns the cards to their base offset 0, the leaving one is removed below anyway
                transition.Stop();
            }

            if (_leavingCard is not null)
            {
                CardHost.Children.Remove(_leavingCard);
                _leavingCard = null;
            }
        }

        private void RecreateCurrentCard()
        {
            if (_currentCard?.Content is not WhatsNewItem item)
                return;

            FinishTransition();
            CardHost.Children.Remove(_currentCard);
            _currentCard = null;

            ShowCard(item, 0);
        }

        private ContentControl CreateCard(WhatsNewItem item) => new()
        {
            Content = item,
            ContentTemplate = CardTemplate ?? (DataTemplate)Resources["DefaultCardTemplate"],
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            IsTabStop = false,
            RenderTransform = new TranslateTransform()
        };

        private static DoubleAnimation CreateSlideAnimation(ContentControl card, double from, double to)
        {
            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TransitionDuration,
                EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 6 }
            };

            Storyboard.SetTarget(animation, card.RenderTransform);
            Storyboard.SetTargetProperty(animation, nameof(TranslateTransform.X));

            return animation;
        }

        private void CardHost_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Cards sliding in and out must not be drawn over the list
            CardHost.Clip = new RectangleGeometry { Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height) };
        }

        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is WhatsNewItem item)
                ActionInvoked?.Invoke(this, new WhatsNewItemEventArgs(item));
        }

        #endregion
    }
}

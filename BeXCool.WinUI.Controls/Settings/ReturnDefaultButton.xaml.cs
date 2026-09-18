using BeXCool.Toolkit.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

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

        // One entry per INotifyPropertyChanged node along the path, paired with the segment we care
        // about on it. Covers both a whole sub-object being swapped and a leaf value changing.
        private readonly List<(INotifyPropertyChanged Source, string Segment)> _pathSubscriptions = new();

        // The (reference-typed) resolved value itself, for in-place sub-property changes.
        private INotifyPropertyChanged _valueSubscription;

        public ReturnDefaultButton()
        {
            System.Diagnostics.Debug.WriteLine("[ReturnDefaultButton] constructed");
            InitializeComponent();

            // Click is already wired up in XAML - adding it here too would reset (and log) twice.
            Loaded += (s, e) => { System.Diagnostics.Debug.WriteLine($"[ReturnDefaultButton] Loaded('{BoundPropertyName}')"); RefreshBindings(); };
            Unloaded += OnUnloaded;
            DataContextChanged += (s, e) => { System.Diagnostics.Debug.WriteLine($"[ReturnDefaultButton] DataContextChanged('{BoundPropertyName}'): new DataContext={DataContext}"); RefreshBindings(); };
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // XAML gives no ordering guarantee between Unloaded and Loaded. When the button is moved
            // between parents - a SwitchPresenter swapping pages, an ItemsRepeater re-realizing an
            // item, a parent with an implicit hide animation (which delays Unloaded by the length of
            // the animation) - the stale Unloaded can arrive *after* the new Loaded. Unsubscribing
            // there would leave a live, visible button with no subscriptions: it would keep whatever
            // visibility it had and never react to the setting again.
            //
            // So defer the teardown and only do it if we really did leave the tree.
            var queue = DispatcherQueue;

            if (queue is null)
            {
                Unsubscribe();
                return;
            }

            queue.TryEnqueue(() =>
            {
                if (IsLoaded) return;

                System.Diagnostics.Debug.WriteLine($"[ReturnDefaultButton] Unloaded('{BoundPropertyName}'): unsubscribing");
                Unsubscribe();
            });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DataContext?.ResetToDefault(BoundPropertyName);
            UpdateVisibility();
        }

        private static void OnBoundPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ReturnDefaultButton button)
                button.RefreshBindings();
        }

        private void RefreshBindings()
        {
            System.Diagnostics.Debug.WriteLine($"[ReturnDefaultButton] RefreshBindings('{BoundPropertyName}'): DataContext={DataContext}");

            Unsubscribe();

            if (DataContext is null || string.IsNullOrWhiteSpace(BoundPropertyName))
            {
                System.Diagnostics.Debug.WriteLine($"[ReturnDefaultButton] RefreshBindings('{BoundPropertyName}'): bailing out (DataContext null or no BoundPropertyName)");
                Visibility = Visibility.Collapsed;
                return;
            }

            var parts = BoundPropertyName.Split('.');
            object node = DataContext;

            for (int i = 0; i < parts.Length; i++)
            {
                if (node is INotifyPropertyChanged inpc)
                {
                    inpc.PropertyChanged += OnPathChanged;
                    _pathSubscriptions.Add((inpc, parts[i]));
                }

                if (i == parts.Length - 1)
                    break;

                var step = node.GetType().GetProperty(parts[i]);
                if (step is null)
                    break;

                node = step.GetValue(node);
                if (node is null)
                    break;
            }

            var (owner, prop) = SettingsReflection.ResolvePath(DataContext, BoundPropertyName);
            if (prop?.GetValue(owner) is INotifyPropertyChanged value)
            {
                _valueSubscription = value;
                _valueSubscription.PropertyChanged += OnValueChanged;
            }

            UpdateVisibility();
        }

        private void OnPathChanged(object sender, PropertyChangedEventArgs e)
        {
            bool matches = string.IsNullOrEmpty(e.PropertyName)
                || _pathSubscriptions.Any(x => ReferenceEquals(x.Source, sender) && x.Segment == e.PropertyName);
            System.Diagnostics.Debug.WriteLine($"[ReturnDefaultButton] OnPathChanged('{BoundPropertyName}'): sender={sender}, propName={e.PropertyName}, matches={matches}, subCount={_pathSubscriptions.Count}");

            // A watched segment (or the whole object) changed - the path may now resolve differently.
            if (matches)
            {
                RefreshBindings();
            }
        }

        private void OnValueChanged(object sender, PropertyChangedEventArgs e) => UpdateVisibility();

        private void Unsubscribe()
        {
            foreach (var (source, _) in _pathSubscriptions)
                source.PropertyChanged -= OnPathChanged;
            _pathSubscriptions.Clear();

            if (_valueSubscription is not null)
            {
                _valueSubscription.PropertyChanged -= OnValueChanged;
                _valueSubscription = null;
            }
        }

        private void UpdateVisibility()
        {
            try
            {
                bool atDefault = DataContext.IsAtDefault(BoundPropertyName);
                System.Diagnostics.Debug.WriteLine($"[ReturnDefaultButton] UpdateVisibility('{BoundPropertyName}'): DataContext={DataContext}, atDefault={atDefault}");
                Visibility = atDefault
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ReturnDefaultButton] IsAtDefault('{BoundPropertyName}') failed: {ex}");
                Visibility = Visibility.Collapsed;
            }
        }
    }
}

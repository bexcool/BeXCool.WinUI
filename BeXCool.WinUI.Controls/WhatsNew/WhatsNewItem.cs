using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.Windows.Input;

namespace BeXCool.WinUI.Controls
{
    /// <summary>
    /// One entry of a <see cref="WhatsNewView"/>. The list shows <see cref="Title"/> with the short <see cref="Description"/>,
    /// the card shows the hero (<see cref="ImageSource"/>, or <see cref="Glyph"/> on an accent gradient), the <see cref="Title"/>,
    /// the longer <see cref="Text"/> and an action button when <see cref="ActionText"/> is set.
    /// </summary>
    public partial class WhatsNewItem : DependencyObject
    {
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(WhatsNewItem), new PropertyMetadata(null));

        /// <summary>
        /// Short text under the title in the list. The card falls back to it when <see cref="Text"/> is not set.
        /// </summary>
        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(WhatsNewItem), new PropertyMetadata(null));

        /// <summary>
        /// Full text of the card.
        /// </summary>
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(WhatsNewItem), new PropertyMetadata(null));

        /// <summary>
        /// Picture at the top of the card. Animated GIFs play, so it can show the feature in action.
        /// </summary>
        public ImageSource ImageSource
        {
            get => (ImageSource)GetValue(ImageSourceProperty);
            set => SetValue(ImageSourceProperty, value);
        }

        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register(nameof(ImageSource), typeof(ImageSource), typeof(WhatsNewItem), new PropertyMetadata(null));

        /// <summary>
        /// Segoe Fluent Icons glyph shown on an accent gradient at the top of the card when there is no <see cref="ImageSource"/>.
        /// </summary>
        public string Glyph
        {
            get => (string)GetValue(GlyphProperty);
            set => SetValue(GlyphProperty, value);
        }

        public static readonly DependencyProperty GlyphProperty =
            DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(WhatsNewItem), new PropertyMetadata(null));

        /// <summary>
        /// Content of the action button. The button is hidden while it's empty.
        /// </summary>
        public string ActionText
        {
            get => (string)GetValue(ActionTextProperty);
            set => SetValue(ActionTextProperty, value);
        }

        public static readonly DependencyProperty ActionTextProperty =
            DependencyProperty.Register(nameof(ActionText), typeof(string), typeof(WhatsNewItem), new PropertyMetadata(null));

        public ICommand ActionCommand
        {
            get => (ICommand)GetValue(ActionCommandProperty);
            set => SetValue(ActionCommandProperty, value);
        }

        public static readonly DependencyProperty ActionCommandProperty =
            DependencyProperty.Register(nameof(ActionCommand), typeof(ICommand), typeof(WhatsNewItem), new PropertyMetadata(null));

        public object ActionCommandParameter
        {
            get => GetValue(ActionCommandParameterProperty);
            set => SetValue(ActionCommandParameterProperty, value);
        }

        public static readonly DependencyProperty ActionCommandParameterProperty =
            DependencyProperty.Register(nameof(ActionCommandParameter), typeof(object), typeof(WhatsNewItem), new PropertyMetadata(null));

        /// <summary>
        /// Hide the <see cref="WhatsNewDialog"/> when the action button is clicked. True by default, false keeps the dialog open,
        /// e.g. for an action whose result the user should see right away. The dialog hides only when its <see cref="WhatsNewDialog.CloseOnAction"/> is true as well.
        /// </summary>
        public bool CloseOnAction
        {
            get => (bool)GetValue(CloseOnActionProperty);
            set => SetValue(CloseOnActionProperty, value);
        }

        public static readonly DependencyProperty CloseOnActionProperty =
            DependencyProperty.Register(nameof(CloseOnAction), typeof(bool), typeof(WhatsNewItem), new PropertyMetadata(true));

        /// <summary>
        /// Version the item came with, shown in a grey pill in the list. New items show <see cref="WhatsNewView.NewBadgeText"/> instead.
        /// </summary>
        public string Version
        {
            get => (string)GetValue(VersionProperty);
            set => SetValue(VersionProperty, value);
        }

        public static readonly DependencyProperty VersionProperty =
            DependencyProperty.Register(nameof(Version), typeof(string), typeof(WhatsNewItem), new PropertyMetadata(null));

        /// <summary>
        /// Shows the accent "New" pill in the list instead of the <see cref="Version"/>.
        /// Set by <see cref="WhatsNewController"/> for the items of the current version, readable for custom templates.
        /// </summary>
        public bool IsNew
        {
            get => (bool)GetValue(IsNewProperty);
            internal set => SetValue(IsNewProperty, value);
        }

        public static readonly DependencyProperty IsNewProperty =
            DependencyProperty.Register(nameof(IsNew), typeof(bool), typeof(WhatsNewItem), new PropertyMetadata(false));

        /// <summary>
        /// Any data to identify the item by, e.g. in <see cref="WhatsNewView.ActionInvoked"/>.
        /// </summary>
        public object Tag
        {
            get => GetValue(TagProperty);
            set => SetValue(TagProperty, value);
        }

        public static readonly DependencyProperty TagProperty =
            DependencyProperty.Register(nameof(Tag), typeof(object), typeof(WhatsNewItem), new PropertyMetadata(null));

        // Narrator reads list items by their ToString()
        public override string ToString() => Title ?? base.ToString();
    }

    public class WhatsNewItemEventArgs : System.EventArgs
    {
        public WhatsNewItem Item { get; }

        public WhatsNewItemEventArgs(WhatsNewItem item)
        {
            Item = item;
        }
    }
}

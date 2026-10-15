using BeXCool.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BeXCool.WinUI.TestingApp
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private static readonly string WhatsNewCacheFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BeXCool.WinUI.TestingApp");

        private int _whatsNewActionCount;

        public MainWindow()
        {
            InitializeComponent();

            UpdateWhatsNewStatus();
        }

        #region What's new

        /// <summary>
        /// Controller for the version in <see cref="WhatsNewVersionTextBox"/>. Null when the version is not valid, the status then shows why.
        /// </summary>
        private WhatsNewController? CreateWhatsNew()
        {
            var options = new WhatsNewOptions(WhatsNewVersionTextBox.Text, WhatsNewCacheFolder);
            foreach (var item in CreateWhatsNewItems())
                options.Items.Add(item);

            try
            {
                return new WhatsNewController(options);
            }
            catch (ArgumentException ex)
            {
                WhatsNewStatusTextBlock.Text = ex.Message;
                return null;
            }
        }

        /// <summary>
        /// One item for each case of the list and the card - new and old versions, hero with a glyph, an image or none,
        /// a long text, an action that closes the dialog and one that keeps it open.
        /// </summary>
        private IEnumerable<WhatsNewItem> CreateWhatsNewItems()
        {
            yield return new WhatsNewItem
            {
                Version = "1.4",
                Title = "Action keeps the dialog open",
                Description = "CloseOnAction is false.",
                Text = "Click the button a few times, the counter in the window behind the dialog goes up and the dialog stays open.",
                Glyph = "",
                ActionText = "Count",
                ActionCommand = new ActionCommand(() => OnWhatsNewAction("Count")),
                CloseOnAction = false
            };

            yield return new WhatsNewItem
            {
                Version = "1.4.1",
                Title = "Action closes the dialog",
                Description = "CloseOnAction is true by default.",
                Text = "The button runs its command and closes the dialog.",
                Glyph = "",
                ActionText = "Run and close",
                ActionCommand = new ActionCommand(() => OnWhatsNewAction("Run and close"))
            };

            yield return new WhatsNewItem
            {
                Version = "1.4",
                Title = "Long text",
                Description = "The card text scrolls.",
                Text = string.Join("\n\n", Enumerable.Range(1, 8).Select(i =>
                    $"Paragraph {i}. Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.")),
                Glyph = ""
            };

            yield return new WhatsNewItem
            {
                Version = "1.3",
                Title = "Image hero",
                Description = "Older version shows its version pill.",
                Text = "The image covers the glyph. Images come from the package, so the glyph is all you see when the app runs unpackaged.",
                ImageSource = new BitmapImage(new Uri("ms-appx:///Images/Wide310x150Logo.png")),
                Glyph = ""
            };

            yield return new WhatsNewItem
            {
                Version = "1.0",
                Title = "No hero, no text",
                Description = "Without an image or glyph the hero is hidden, the card falls back to this description."
            };

            yield return new WhatsNewItem
            {
                Title = "No version",
                Description = "Neither pill is shown.",
                Glyph = ""
            };
        }

        private void UpdateWhatsNewStatus(WhatsNewController? whatsNew = null)
        {
            whatsNew ??= CreateWhatsNew();
            if (whatsNew is null)
                return;

            WhatsNewStatusTextBlock.Text =
                $"Current version: {whatsNew.CurrentVersion}, last seen: {whatsNew.LastSeenVersion?.ToString() ?? "never"}\n" +
                $"Has new items: {whatsNew.HasNewItems}, should show: {whatsNew.ShouldShowWhatsNew}\n" +
                $"Cache: {whatsNew.CacheFilePath}";
        }

        private void OnWhatsNewAction(string action)
        {
            _whatsNewActionCount++;
            WhatsNewActionTextBlock.Text = $"Actions invoked: {_whatsNewActionCount}, last: {action}";
        }

        private void WhatsNewVersionTextBox_TextChanged(object sender, TextChangedEventArgs e) => UpdateWhatsNewStatus();

        private async void ShowWhatsNewButton_Click(object sender, RoutedEventArgs e)
        {
            var whatsNew = CreateWhatsNew();
            if (whatsNew is null)
                return;

            await whatsNew.ShowDialogAsync((FrameworkElement)Content);
            UpdateWhatsNewStatus(whatsNew);
        }

        /// <summary>
        /// What an application does on start - shows the dialog only once per version (major and minor).
        /// </summary>
        private async void ShowWhatsNewIfNewButton_Click(object sender, RoutedEventArgs e)
        {
            var whatsNew = CreateWhatsNew();
            if (whatsNew is null)
                return;

            if (whatsNew.ShouldShowWhatsNew)
                await whatsNew.ShowDialogAsync((FrameworkElement)Content);

            UpdateWhatsNewStatus(whatsNew);
        }

        private void ForgetWhatsNewButton_Click(object sender, RoutedEventArgs e)
        {
            string cacheFilePath = Path.Combine(WhatsNewCacheFolder, WhatsNewController.CacheFileName);
            if (File.Exists(cacheFilePath))
                File.Delete(cacheFilePath);

            UpdateWhatsNewStatus();
        }

        // Partial for the WinRT interop code CsWinRT generates for ICommand
        private sealed partial class ActionCommand(Action execute) : ICommand
        {
            // Always executable
            public event EventHandler? CanExecuteChanged { add { } remove { } }

            public bool CanExecute(object? parameter) => true;

            public void Execute(object? parameter) => execute();
        }

        #endregion
    }
}

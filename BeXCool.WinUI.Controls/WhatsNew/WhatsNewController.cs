using BeXCool.Toolkit.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using Windows.Foundation;

namespace BeXCool.WinUI.Controls
{
    /// <summary>
    /// Runs the whole "What's new" system from <see cref="WhatsNewOptions"/>: marks the items of the current version as new,
    /// tells whether the user should see them (<see cref="ShouldShowWhatsNew"/>), shows them in a <see cref="WhatsNewDialog"/>
    /// and remembers the last seen version in <see cref="CacheFileName"/>.
    /// </summary>
    public class WhatsNewController
    {
        public const string CacheFileName = "WhatsNewCache.json";

        public WhatsNewOptions Options { get; }

        /// <summary>
        /// <see cref="WhatsNewOptions.Version"/> of the application.
        /// </summary>
        public Version CurrentVersion { get; }

        /// <summary>
        /// Version the user has seen "What's new" for last time. Null when they never have, or the cache can't be read.
        /// </summary>
        public Version LastSeenVersion { get; private set; }

        public string CacheFilePath { get; }

        /// <summary>
        /// There is something to show at all, e.g. for the visibility of a "What's new" button.
        /// </summary>
        public bool HasItems => Options.Items.Count > 0;

        /// <summary>
        /// Some items came with the current version (major and minor).
        /// </summary>
        public bool HasNewItems => Options.Items.Any(IsNewItem);

        /// <summary>
        /// There are new items and the user hasn't seen "What's new" since the update to the current version (major and minor).
        /// Also true without the cache, i.e. on the first run or in the first version with "What's new".
        /// </summary>
        public bool ShouldShowWhatsNew => HasNewItems && (LastSeenVersion is null || CompareRelease(LastSeenVersion, CurrentVersion) < 0);

        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the version of <paramref name="options"/> is not valid or the cache folder is not set.</exception>
        public WhatsNewController(WhatsNewOptions options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));

            if (!TryParseVersion(options.Version, out var version))
                throw new ArgumentException($"'{options.Version}' is not a valid version.", nameof(options));

            if (string.IsNullOrWhiteSpace(options.CacheFolder))
                throw new ArgumentException("The cache folder is not set.", nameof(options));

            CurrentVersion = version;
            CacheFilePath = Path.Combine(options.CacheFolder, CacheFileName);
            LastSeenVersion = LoadLastSeenVersion();
        }

        /// <summary>
        /// The item came with the current version (major and minor) and shows the "New" pill.
        /// </summary>
        public bool IsNewItem(WhatsNewItem item)
            => item is not null && TryParseVersion(item.Version, out var version) && CompareRelease(version, CurrentVersion) == 0;

        /// <summary>
        /// Shows the items in a <see cref="WhatsNewDialog"/> in the window of <paramref name="root"/>, in its theme.
        /// The version is remembered as seen once the dialog opens.
        /// </summary>
        /// <param name="root">Loaded element in the window, usually its content.</param>
        public IAsyncOperation<ContentDialogResult> ShowDialogAsync(FrameworkElement root)
        {
            ArgumentNullException.ThrowIfNull(root);

            var dialog = CreateDialog();
            dialog.XamlRoot = root.XamlRoot;
            // The dialog doesn't take the theme of the window content by itself
            dialog.RequestedTheme = root.ActualTheme;

            return dialog.ShowAsync();
        }

        /// <summary>
        /// Creates a <see cref="WhatsNewDialog"/> with the items and texts of <see cref="Options"/>, to adjust it before showing.
        /// Set <see cref="ContentDialog.XamlRoot"/> before calling <see cref="ContentDialog.ShowAsync"/>.
        /// The version is remembered as seen once the dialog opens.
        /// </summary>
        public WhatsNewDialog CreateDialog()
        {
            foreach (var item in Options.Items)
                item.IsNew = IsNewItem(item);

            WhatsNewView.NewBadgeText = Options.NewBadgeText;

            var dialog = new WhatsNewDialog
            {
                Header = Options.Header,
                CloseButtonToolTip = Options.CloseButtonToolTip,
                ItemsSource = Options.Items
            };
            dialog.Opened += (s, e) => MarkAsSeen();

            return dialog;
        }

        /// <summary>
        /// Remembers the current version as seen, so <see cref="ShouldShowWhatsNew"/> is false until the next version.
        /// The dialog of this controller calls it when it opens, call it yourself when the user gets to the news some other way.
        /// </summary>
        /// <returns>False when the cache couldn't be written, the version is then seen only until the application closes.</returns>
        public bool MarkAsSeen()
        {
            LastSeenVersion = CurrentVersion;

            try
            {
                Directory.CreateDirectory(Options.CacheFolder);

                var cache = new WhatsNewCache { LastSeenVersion = CurrentVersion.ToString() };
                return SafeFile.WriteAllText(CacheFilePath, JsonConvert.SerializeObject(cache, Formatting.Indented));
            }
            catch (Exception)
            {
                return false;
            }
        }

        private Version LoadLastSeenVersion()
        {
            try
            {
                if (!File.Exists(CacheFilePath))
                    return null;

                var cache = JsonConvert.DeserializeObject<WhatsNewCache>(File.ReadAllText(CacheFilePath));
                return TryParseVersion(cache?.LastSeenVersion, out var version) ? version : null;
            }
            catch (Exception)
            {
                // A broken cache is the same as none, it's written again once the user sees the news
                return null;
            }
        }

        private static bool TryParseVersion(string text, out Version version)
        {
            version = null;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            text = text.Trim();

            // Version needs at least the major and minor part, "2" is taken as "2.0"
            return Version.TryParse(text.Contains('.') ? text : text + ".0", out version);
        }

        /// <summary>
        /// Compares only the major and minor version - the items are new for the whole release, not for a single build.
        /// </summary>
        private static int CompareRelease(Version a, Version b)
            => a.Major != b.Major ? a.Major.CompareTo(b.Major) : a.Minor.CompareTo(b.Minor);
    }
}

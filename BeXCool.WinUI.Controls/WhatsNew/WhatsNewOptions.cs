using System.Collections.Generic;

namespace BeXCool.WinUI.Controls
{
    /// <summary>
    /// Everything a <see cref="WhatsNewController"/> is created with - the version, where to keep the cache, the items and the texts of the dialog.
    /// </summary>
    public class WhatsNewOptions
    {
        /// <summary>
        /// Current version of the application, e.g. "1.3.0.0". Items with the same major and minor version are new.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Folder of <see cref="WhatsNewController.CacheFileName"/>, which remembers the last version the user has seen "What's new" for.
        /// It's created when it doesn't exist.
        /// </summary>
        public string CacheFolder { get; set; }

        /// <summary>
        /// Items of the "What's new" dialog, the newest first.
        /// </summary>
        public IList<WhatsNewItem> Items { get; } = new List<WhatsNewItem>();

        /// <summary>
        /// Header text of the "What's new" dialog.
        /// </summary>
        public string Header { get; set; } = "What's new";

        /// <summary>
        /// Tooltip of the close button of the "What's new" dialog.
        /// </summary>
        public string CloseButtonToolTip { get; set; } = "Close";

        /// <summary>
        /// Text of the pill of new items.
        /// </summary>
        public string NewBadgeText { get; set; } = "New";

        public WhatsNewOptions()
        {
        }

        /// <param name="version">Current version of the application.</param>
        /// <param name="cacheFolder">Folder of the cache file.</param>
        public WhatsNewOptions(string version, string cacheFolder)
        {
            Version = version;
            CacheFolder = cacheFolder;
        }
    }
}

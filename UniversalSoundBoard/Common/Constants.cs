using System.Collections.Generic;
using Windows.UI;

namespace UniversalSoundboard.Common
{
    public class Constants
    {
        #region Design constants
        public const int mobileMaxWidth = 775;
        public const int topButtonsCollapsedMaxWidth = 1800;
        public const int sideBarCollapsedMaxWidth = 1100;
        public const int hideSearchBoxMaxWidth = 800;
        #endregion

        #region Colors for the background of PlayingSoundsBar and SideBar
        public const double sideBarAcrylicBackgroundTintOpacity = 0.6;
        public const double playingSoundsBarAcrylicBackgroundTintOpacity = 0.85;
        public const double secondaryWindowAcrylicBackgroundTintOpacity = 0.85;
        public static readonly Color sideBarLightBackgroundColor = Color.FromArgb(255, 245, 245, 245);            // #f5f5f5
        public static readonly Color sideBarDarkBackgroundColor = Color.FromArgb(255, 29, 34, 49);                // #1d2231
        public static readonly Color playingSoundsBarLightBackgroundColor = Color.FromArgb(255, 253, 253, 253);   // #fdfdfd
        public static readonly Color playingSoundsBarDarkBackgroundColor = Color.FromArgb(255, 15, 20, 35);       // #0f1423
        public static readonly Color secondaryWindowLightBackgroundColor = Color.FromArgb(255, 255, 255, 255);    // #ffffff
        public static readonly Color secondaryWindowDarkBackgroundColor = Color.FromArgb(255, 13, 18, 33);        // #0d1221
        #endregion

        #region URLs
        public const string WebsiteBaseUrl = "https://dav-login-7ymir.ondigitalocean.app";
        public const string UniversalSoundboardWebsiteBaseUrl = "https://universalsoundboard.dav-apps.tech";
        public const string ApiBaseUrl = "https://universalsoundboard-api-rmkdv.ondigitalocean.app";
        #endregion

        #region dav Keys
        public static string ApiKey { get => Env.DavApiKey; }
        public const int AppId = 1;
        public const int SoundFileTableId = 6;
        public const int ImageFileTableId = 7;
        public const int CategoryTableId = 8;
        public const int SoundTableId = 5;
        public const int PlayingSoundTableId = 9;
        public const int OrderTableId = 12;
        #endregion

        #region Table property names
        public const string SoundTableNamePropertyName = "name";
        public const string SoundTableFavouritePropertyName = "favourite";
        public const string SoundTableSoundUuidPropertyName = "sound_uuid";
        public const string SoundTableImageUuidPropertyName = "image_uuid";
        public const string SoundTableCategoryUuidPropertyName = "category_uuid";
        public const string SoundTableDefaultVolumePropertyName = "default_volume";
        public const string SoundTableDefaultMutedPropertyName = "default_muted";
        public const string SoundTableDefaultPlaybackSpeedPropertyName = "default_playback_speed";
        public const string SoundTableDefaultRepetitionsPropertyName = "default_repetitions";
        public const string SoundTableDefaultOutputDevicePropertyName = "default_output_device";
        public const string SoundTableHotkeysPropertyName = "hotkeys";
        public const string SoundTableSourcePropertyName = "source";

        public const string CategoryTableParentPropertyName = "parent";
        public const string CategoryTableNamePropertyName = "name";
        public const string CategoryTableIconPropertyName = "icon";

        public const string PlayingSoundTableSoundIdsPropertyName = "sound_ids";
        public const string PlayingSoundTableCurrentPropertyName = "current";
        public const string PlayingSoundTableRepetitionsPropertyName = "repetitions";
        public const string PlayingSoundTableRandomlyPropertyName = "randomly";
        public const string PlayingSoundTableVolumePropertyName = "volume2";
        public const string PlayingSoundTableMutedPropertyName = "muted";
        public const string PlayingSoundTableOutputDevicePropertyName = "output_device";
        public const string PlayingSoundTablePlaybackSpeedPropertyName = "playback_speed";

        public const string OrderTableTypePropertyName = "type";
        public const string OrderTableCategoryPropertyName = "category";
        public const string OrderTableFavouritePropertyName = "favs";
        #endregion

        #region Other constants
        public const string TableObjectExtPropertyName = "ext";
        public const string CategoryOrderType = "0";
        public const string SoundOrderType = "1";

        public const string ImportFolderName = "import";
        public const string ImportZipFileName = "import.zip";
        public const string ExportFolderName = "export";
        public const string ExportZipFileName = "export.zip";
        public const string ExportDataFileName = "data.json";
        public const string TileFolderName = "tile";

        public const string FluentIconsFontFamily = "/Assets/Fonts/SegoeFluentIcons.ttf#Segoe Fluent Icons";
        public const string DefaultProfileImageUrl = "https://dav-backend.fra1.cdn.digitaloceanspaces.com/profileImages/default.png";
        public const string UniversalSoundboardPlusAddonStoreId = "9NRQTG6ZVDVX";
        public const string CreateCheckoutSessionSuccessUrl = "https://universalsoundboard.dav-apps.tech/upgrade?success=true&plan=1";
        public const string CreateCheckoutSessionCancelUrl = "https://universalsoundboard.dav-apps.tech/upgrade?success=false";

        public static readonly List<string> allowedFileTypes = new List<string>
        {
            ".mp3",
            ".m4a",
            ".wav",
            ".ogg",
            ".wma",
            ".flac"
        };

        public static readonly List<string> allowedAudioMimeTypes = new List<string>
        {
            "audio/mpeg",   // .mp3
            "audio/mp4",    // .m4a
            "audio/wav",    // .wav
            "audio/ogg"     // .ogg
        };
        #endregion
    }
}

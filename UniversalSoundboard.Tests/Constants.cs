namespace UniversalSoundboard.Tests
{
    internal static class Constants
    {
        #region Other constants
        internal const string testerXTestAppAccessToken = "ckktuu0gs00008iw3ctnrofzf";
        internal const string CategoryOrderType = "0";
        internal const string SoundOrderType = "1";
        #endregion

        #region dav app & table IDs
        internal const int AppId = 1;
        internal const int SoundFileTableId = 6;
        internal const int ImageFileTableId = 7;
        internal const int CategoryTableId = 8;
        internal const int SoundTableId = 5;
        internal const int PlayingSoundTableId = 9;
        internal const int OrderTableId = 12;
        #endregion

        #region Table property names
        internal const string SoundTableName = "Sound";
        internal const string SoundTableNamePropertyName = "name";
        internal const string SoundTableFavouritePropertyName = "favourite";
        internal const string SoundTableSoundUuidPropertyName = "sound_uuid";
        internal const string SoundTableImageUuidPropertyName = "image_uuid";
        internal const string SoundTableCategoryUuidPropertyName = "category_uuid";
        internal const string SoundTableDefaultVolumePropertyName = "default_volume";
        internal const string SoundTableDefaultMutedPropertyName = "default_muted";
        internal const string SoundTableDefaultPlaybackSpeedPropertyName = "default_playback_speed";
        internal const string SoundTableDefaultRepetitionsPropertyName = "default_repetitions";
        internal const string SoundTableDefaultOutputDevicePropertyName = "default_output_device";
        internal const string SoundTableHotkeysPropertyName = "hotkeys";
        internal const string SoundTableSourcePropertyName = "source";

        internal const string CategoryTableName = "Category";
        internal const string CategoryTableParentPropertyName = "parent";
        internal const string CategoryTableNamePropertyName = "name";
        internal const string CategoryTableIconPropertyName = "icon";

        internal const string PlayingSoundTableName = "PlayingSound";
        internal const string PlayingSoundTableSoundIdsPropertyName = "sound_ids";
        internal const string PlayingSoundTableCurrentPropertyName = "current";
        internal const string PlayingSoundTableRepetitionsPropertyName = "repetitions";
        internal const string PlayingSoundTableRandomlyPropertyName = "randomly";
        internal const string PlayingSoundTableVolumePropertyName = "volume2";
        internal const string PlayingSoundTableMutedPropertyName = "muted";
        internal const string PlayingSoundTableOutputDevicePropertyName = "output_device";
        internal const string PlayingSoundTablePlaybackSpeedPropertyName = "playback_speed";

        internal const string OrderTableName = "Order";
        internal const string OrderTableTypePropertyName = "type";
        internal const string OrderTableCategoryPropertyName = "category";
        internal const string OrderTableFavouritePropertyName = "favs";

        internal const string SoundFileTableName = "SoundFile";
        internal const string ImageFileTableName = "ImageFile";
        #endregion
    }
}

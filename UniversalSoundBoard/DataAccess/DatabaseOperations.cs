using davClassLibrary;
using davClassLibrary.Models;
using System;
using System.Collections.Generic;
using System.IO;
using Windows.Storage;

namespace UniversalSoundBoard.DataAccess
{
    class DatabaseOperations
    {
        #region General Methods
        public static TableObject GetObject(Guid uuid)
        {
            return Dav.Database.GetTableObject(uuid);
        }

        public static void DeleteObject(Guid uuid)
        {
            Dav.Database.DeleteTableObject(uuid);
        }
        #endregion

        #region Sound
        public static void AddSound(Guid uuid, string name, string soundUuid, string soundExt, string categoryUuid)
        {
            // Create TableObject with sound informations and TableObject with the Soundfile
            var soundTableObject = new TableObject(uuid, FileManager.SoundTableId);
            soundTableObject.AddProperty(FileManager.SoundTableNamePropertyName, name);
            soundTableObject.AddProperty(FileManager.SoundTableFavouritePropertyName, false.ToString());
            soundTableObject.AddProperty(FileManager.SoundTableSoundUuidPropertyName, soundUuid);
            soundTableObject.AddProperty(FileManager.SoundTableSoundExtPropertyName, soundExt);
            if(!String.IsNullOrEmpty(categoryUuid))
                soundTableObject.AddProperty(FileManager.SoundTableCategoryUuidPropertyName, categoryUuid);
        }

        public static List<TableObject> GetAllSounds()
        {
            return Dav.Database.GetAllTableObjects(FileManager.SoundTableId);
        }

        public static void UpdateSound(Guid uuid, string name, string favourite, string soundUuid, string soundExt, string imageUuid, string imageExt, string categoryUuid)
        {
            // Get the sound table object
            var soundTableObject = Dav.Database.GetTableObject(uuid);

            UpdatePropertyOfTableObject(soundTableObject, FileManager.SoundTableNamePropertyName, name);
            UpdatePropertyOfTableObject(soundTableObject, FileManager.SoundTableFavouritePropertyName, favourite);
            UpdatePropertyOfTableObject(soundTableObject, FileManager.SoundTableSoundUuidPropertyName, soundUuid);
            UpdatePropertyOfTableObject(soundTableObject, FileManager.SoundTableSoundExtPropertyName, soundExt);
            UpdatePropertyOfTableObject(soundTableObject, FileManager.SoundTableImageUuidPropertyName, imageUuid);
            UpdatePropertyOfTableObject(soundTableObject, FileManager.SoundTableImageExtPropertyName, imageExt);
            UpdatePropertyOfTableObject(soundTableObject, FileManager.SoundTableCategoryUuidPropertyName, categoryUuid);
        }
        #endregion

        #region SoundFile
        public static void AddSoundFile(Guid uuid, StorageFile audioFile)
        {
            Dav.Database.CreateTableObject(new TableObject(uuid, FileManager.SoundFileTableId, new FileInfo(audioFile.Path)));
        }

        public static List<TableObject> GetAllSoundFiles()
        {
            return Dav.Database.GetAllTableObjects(FileManager.SoundFileTableId);
        }
        #endregion SoundFile

        #region ImageFile
        public static void AddImageFile(Guid uuid, StorageFile imageFile)
        {
            Dav.Database.CreateTableObject(new TableObject(uuid, FileManager.ImageFileTableId, new FileInfo(imageFile.Path)));
        }

        public static void UpdateImageFile(Guid uuid, StorageFile imageFile)
        {
            var imageFileTableObject = Dav.Database.GetTableObject(uuid);

            if(imageFileTableObject != null)
                imageFileTableObject.File = new FileInfo(imageFile.Path);
        }
        #endregion

        #region Category
        public static void AddCategory(Guid uuid, string name, string icon)
        {
            var categoryTableObject = new TableObject(uuid, FileManager.CategoryTableId);
            categoryTableObject.AddProperty(FileManager.CategoryTableNamePropertyName, name);
            categoryTableObject.AddProperty(FileManager.CategoryTableIconPropertyName, icon);
        }

        public static List<TableObject> GetAllCategories()
        {
            return Dav.Database.GetAllTableObjects(FileManager.CategoryTableId);
        }

        public static void UpdateCategory(Guid uuid, string name, string icon)
        {
            var categoryTableObject = Dav.Database.GetTableObject(uuid);

            UpdatePropertyOfTableObject(categoryTableObject, FileManager.CategoryTableNamePropertyName, name);
            UpdatePropertyOfTableObject(categoryTableObject, FileManager.CategoryTableIconPropertyName, icon);
        }
        #endregion

        #region PlayingSound
        public static void AddPlayingSound(Guid uuid, List<string> soundIds, int current, int repetitions, bool randomly, double volume)
        {
            var playingSoundTableObject = new TableObject(uuid, FileManager.PlayingsoundTableId);
            playingSoundTableObject.AddProperty(FileManager.PlayingSoundTableSoundIdsPropertyName, ConvertIdListToString(soundIds));
            playingSoundTableObject.AddProperty(FileManager.PlayingSoundTableCurrentPropertyName, current.ToString());
            playingSoundTableObject.AddProperty(FileManager.PlayingSoundTableRepetitionsPropertyName, repetitions.ToString());
            playingSoundTableObject.AddProperty(FileManager.PlayingSoundTableRandomlyPropertyName, randomly.ToString());
            playingSoundTableObject.AddProperty(FileManager.PlayingSoundTableVolumePropertyName, volume.ToString());
        }

        public static List<TableObject> GetAllPlayingSounds()
        {
            return Dav.Database.GetAllTableObjects(FileManager.PlayingsoundTableId);
        }

        public static void UpdatePlayingSound(Guid uuid, List<string> soundIds, string current, string repetitions, string randomly, string volume)
        {
            var playingSoundTableObject = Dav.Database.GetTableObject(uuid);

            if (soundIds != null)
                UpdatePropertyOfTableObject(playingSoundTableObject, FileManager.PlayingSoundTableSoundIdsPropertyName, ConvertIdListToString(soundIds));
            UpdatePropertyOfTableObject(playingSoundTableObject, FileManager.PlayingSoundTableCurrentPropertyName, current);
            UpdatePropertyOfTableObject(playingSoundTableObject, FileManager.PlayingSoundTableRepetitionsPropertyName, repetitions);
            UpdatePropertyOfTableObject(playingSoundTableObject, FileManager.PlayingSoundTableRandomlyPropertyName, randomly);
            UpdatePropertyOfTableObject(playingSoundTableObject, FileManager.PlayingSoundTableVolumePropertyName, volume);
        }
        #endregion

        // Other methods
        private static string ConvertIdListToString(List<string> ids)
        {
            string idsString = "";
            foreach (string id in ids)
            {
                idsString += id + ",";
            }
            // Remove the last character, which is a ,
            idsString = idsString.Remove(idsString.Length - 1);

            return idsString;
        }

        private static void UpdatePropertyOfTableObject(TableObject tableObject, string propertyName, string newPropertyValue)
        {
            if (String.IsNullOrEmpty(newPropertyValue))
                return;

            var property = tableObject.Properties.Find(prop => prop.Name == propertyName);
            if (property != null)
                property.Value = newPropertyValue;
            else
                tableObject.AddProperty(propertyName, newPropertyValue);
        }
    }
}

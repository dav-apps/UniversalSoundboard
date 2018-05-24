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
            soundTableObject.SetPropertyValue(FileManager.SoundTableNamePropertyName, name);
            soundTableObject.SetPropertyValue(FileManager.SoundTableFavouritePropertyName, bool.FalseString);
            soundTableObject.SetPropertyValue(FileManager.SoundTableSoundUuidPropertyName, soundUuid);
            soundTableObject.SetPropertyValue(FileManager.SoundTableSoundExtPropertyName, soundExt);
            if(!String.IsNullOrEmpty(categoryUuid))
                soundTableObject.SetPropertyValue(FileManager.SoundTableCategoryUuidPropertyName, categoryUuid);
        }

        public static List<TableObject> GetAllSounds()
        {
            return Dav.Database.GetAllTableObjects(FileManager.SoundTableId);
        }

        public static void UpdateSound(Guid uuid, string name, string favourite, string soundUuid, string soundExt, string imageUuid, string imageExt, string categoryUuid)
        {
            // Get the sound table object
            var soundTableObject = Dav.Database.GetTableObject(uuid);

            if (soundTableObject == null) return;
            if (soundTableObject.TableId != FileManager.SoundTableId) return;

            if (!String.IsNullOrEmpty(name))
                soundTableObject.SetPropertyValue(FileManager.SoundTableNamePropertyName, name);
            if (!String.IsNullOrEmpty(favourite))
                soundTableObject.SetPropertyValue(FileManager.SoundTableFavouritePropertyName, favourite);
            if (!String.IsNullOrEmpty(soundUuid))
                soundTableObject.SetPropertyValue(FileManager.SoundTableSoundUuidPropertyName, soundUuid);
            if (!String.IsNullOrEmpty(soundExt))
                soundTableObject.SetPropertyValue(FileManager.SoundTableSoundExtPropertyName, soundExt);
            if (!String.IsNullOrEmpty(imageUuid))
                soundTableObject.SetPropertyValue(FileManager.SoundTableImageUuidPropertyName, imageUuid);
            if (!String.IsNullOrEmpty(imageExt))
                soundTableObject.SetPropertyValue(FileManager.SoundTableImageExtPropertyName, imageExt);
            if (!String.IsNullOrEmpty(categoryUuid))
                soundTableObject.SetPropertyValue(FileManager.SoundTableCategoryUuidPropertyName, categoryUuid);
        }
        
        public static void DeleteSound(Guid uuid)
        {
            var soundTableObject = Dav.Database.GetTableObject(uuid);

            if (soundTableObject == null) return;
            if (soundTableObject.TableId != FileManager.SoundTableId) return;

            // Delete the sound file and the image file
            Guid soundFileUuid = FileManager.ConvertStringToGuid(soundTableObject.GetPropertyValue(FileManager.SoundTableSoundUuidPropertyName));
            Guid imageFileUuid = FileManager.ConvertStringToGuid(soundTableObject.GetPropertyValue(FileManager.SoundTableImageUuidPropertyName));

            if (!Equals(soundFileUuid, Guid.Empty))
                Dav.Database.DeleteTableObject(soundFileUuid);
            if (!Equals(soundFileUuid, Guid.Empty))
                Dav.Database.DeleteTableObject(imageFileUuid);

            // Delete the sound itself
            Dav.Database.DeleteTableObject(soundTableObject);
        }
        #endregion

        #region SoundFile
        public static void AddSoundFile(Guid uuid, StorageFile audioFile)
        {
            var tableObject = new TableObject(uuid, FileManager.SoundFileTableId);
            tableObject.SetFile(new FileInfo(audioFile.Path));
        }

        public static List<TableObject> GetAllSoundFiles()
        {
            return Dav.Database.GetAllTableObjects(FileManager.SoundFileTableId);
        }
        #endregion SoundFile

        #region ImageFile
        public static void AddImageFile(Guid uuid, StorageFile imageFile)
        {
            var tableObject = new TableObject(uuid, FileManager.ImageFileTableId);
            tableObject.SetFile(new FileInfo(imageFile.Path));
        }

        public static void UpdateImageFile(Guid uuid, StorageFile imageFile)
        {
            var imageFileTableObject = Dav.Database.GetTableObject(uuid);

            if (imageFileTableObject == null) return;
            if (imageFileTableObject.TableId != FileManager.ImageFileTableId) return;

            imageFileTableObject.SetFile(new FileInfo(imageFile.Path));
        }
        #endregion

        #region Category
        public static void AddCategory(Guid uuid, string name, string icon)
        {
            var categoryTableObject = new TableObject(uuid, FileManager.CategoryTableId);
            categoryTableObject.SetPropertyValue(FileManager.CategoryTableNamePropertyName, name);
            categoryTableObject.SetPropertyValue(FileManager.CategoryTableIconPropertyName, icon);
        }

        public static List<TableObject> GetAllCategories()
        {
            return Dav.Database.GetAllTableObjects(FileManager.CategoryTableId);
        }

        public static void UpdateCategory(Guid uuid, string name, string icon)
        {
            var categoryTableObject = Dav.Database.GetTableObject(uuid);

            if (categoryTableObject == null) return;
            if (categoryTableObject.TableId != FileManager.CategoryTableId) return;

            if (!String.IsNullOrEmpty(name))
                categoryTableObject.SetPropertyValue(FileManager.CategoryTableNamePropertyName, name);
            if (!String.IsNullOrEmpty(icon))
                categoryTableObject.SetPropertyValue(FileManager.CategoryTableIconPropertyName, icon);
        }
        #endregion

        #region PlayingSound
        public static void AddPlayingSound(Guid uuid, List<string> soundIds, int current, int repetitions, bool randomly, double volume)
        {
            var playingSoundTableObject = new TableObject(uuid, FileManager.PlayingsoundTableId);
            playingSoundTableObject.SetPropertyValue(FileManager.PlayingSoundTableSoundIdsPropertyName, ConvertIdListToString(soundIds));
            playingSoundTableObject.SetPropertyValue(FileManager.PlayingSoundTableCurrentPropertyName, current.ToString());
            playingSoundTableObject.SetPropertyValue(FileManager.PlayingSoundTableRepetitionsPropertyName, repetitions.ToString());
            playingSoundTableObject.SetPropertyValue(FileManager.PlayingSoundTableRandomlyPropertyName, randomly.ToString());
            playingSoundTableObject.SetPropertyValue(FileManager.PlayingSoundTableVolumePropertyName, volume.ToString());
        }

        public static List<TableObject> GetAllPlayingSounds()
        {
            return Dav.Database.GetAllTableObjects(FileManager.PlayingsoundTableId);
        }

        public static void UpdatePlayingSound(Guid uuid, List<string> soundIds, string current, string repetitions, string randomly, string volume)
        {
            var playingSoundTableObject = Dav.Database.GetTableObject(uuid);

            if (playingSoundTableObject == null) return;
            if (playingSoundTableObject.TableId != FileManager.CategoryTableId) return;

            if (soundIds != null)
                playingSoundTableObject.SetPropertyValue(FileManager.PlayingSoundTableSoundIdsPropertyName, ConvertIdListToString(soundIds));
            if (!String.IsNullOrEmpty(current))
                playingSoundTableObject.SetPropertyValue(FileManager.PlayingSoundTableCurrentPropertyName, current);
            if (!String.IsNullOrEmpty(repetitions))
                playingSoundTableObject.SetPropertyValue(FileManager.PlayingSoundTableRepetitionsPropertyName, repetitions);
            if (!String.IsNullOrEmpty(randomly))
                playingSoundTableObject.SetPropertyValue(FileManager.PlayingSoundTableRandomlyPropertyName, randomly);
            if (!String.IsNullOrEmpty(volume))
                playingSoundTableObject.SetPropertyValue(FileManager.PlayingSoundTableVolumePropertyName, volume);
        }
        #endregion

        #region Helper Methods
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
        #endregion
    }
}

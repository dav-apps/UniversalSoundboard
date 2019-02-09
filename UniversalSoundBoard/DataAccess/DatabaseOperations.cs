using davClassLibrary;
using davClassLibrary.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UniversalSoundboard.Models;
using UniversalSoundBoard.Models;
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

        public static bool ObjectExists(Guid uuid)
        {
            return Dav.Database.TableObjectExists(uuid);
        }

        public static void DeleteObject(Guid uuid)
        {
            // Get the object and delete it
            var tableObject = Dav.Database.GetTableObject(uuid);
            if (tableObject != null)
                tableObject.Delete();
        }
        #endregion

        #region Sound
        public static void AddSound(Guid uuid, string name, string soundUuid, string categoryUuid)
        {
            // Create TableObject with sound informations and TableObject with the Soundfile
            var properties = new List<Property>
            {
                new Property{Name = FileManager.SoundTableNamePropertyName, Value = name},
                new Property{Name = FileManager.SoundTableFavouritePropertyName, Value = bool.FalseString},
                new Property{Name = FileManager.SoundTableSoundUuidPropertyName, Value = soundUuid}
            };

            if (!String.IsNullOrEmpty(categoryUuid))
                properties.Add(new Property { Name = FileManager.SoundTableCategoryUuidPropertyName, Value = categoryUuid });

            new TableObject(uuid, FileManager.SoundTableId, properties);
        }

        public static List<TableObject> GetAllSounds()
        {
            return Dav.Database.GetAllTableObjects(FileManager.SoundTableId, false);
        }

        public static void UpdateSound(Guid uuid, string name, string favourite, string soundUuid, string imageUuid, List<string> categoryUuids)
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
            if (!String.IsNullOrEmpty(imageUuid))
                soundTableObject.SetPropertyValue(FileManager.SoundTableImageUuidPropertyName, imageUuid);
            if (categoryUuids != null)
                soundTableObject.SetPropertyValue(FileManager.SoundTableCategoryUuidPropertyName, ConvertIdListToString(categoryUuids));
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
            {
                var soundFileTableObject = Dav.Database.GetTableObject(soundFileUuid);
                if (soundFileTableObject != null)
                    soundFileTableObject.Delete();
            }
            if (!Equals(soundFileUuid, Guid.Empty))
            {
                var imageFileTableObject = Dav.Database.GetTableObject(imageFileUuid);
                if (imageFileTableObject != null)
                    imageFileTableObject.Delete();
            }

            // Delete the sound itself
            soundTableObject.Delete();
        }
        #endregion

        #region SoundFile
        public static void AddSoundFile(Guid uuid, StorageFile audioFile)
        {
            var tableObject = new TableObject(uuid, FileManager.SoundFileTableId, new FileInfo(audioFile.Path));
        }

        public static List<TableObject> GetAllSoundFiles()
        {
            return Dav.Database.GetAllTableObjects(FileManager.SoundFileTableId, false);
        }
        #endregion SoundFile

        #region ImageFile
        public static void AddImageFile(Guid uuid, StorageFile imageFile)
        {
            var tableObject = new TableObject(uuid, FileManager.ImageFileTableId, new FileInfo(imageFile.Path));
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
            List<Property> properties = new List<Property>
            {
                new Property{Name = FileManager.CategoryTableNamePropertyName, Value = name},
                new Property{Name = FileManager.CategoryTableIconPropertyName, Value = icon}
            };
            new TableObject(uuid, FileManager.CategoryTableId, properties);
        }

        public static List<TableObject> GetAllCategories()
        {
            return Dav.Database.GetAllTableObjects(FileManager.CategoryTableId, false);
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
            var properties = new List<Property>
            {
                new Property{Name = FileManager.PlayingSoundTableSoundIdsPropertyName, Value = ConvertIdListToString(soundIds)},
                new Property{Name = FileManager.PlayingSoundTableCurrentPropertyName, Value = current.ToString()},
                new Property{Name = FileManager.PlayingSoundTableRepetitionsPropertyName, Value = repetitions.ToString()},
                new Property{Name = FileManager.PlayingSoundTableRandomlyPropertyName, Value = randomly.ToString()},
                new Property{Name = FileManager.PlayingSoundTableVolumePropertyName, Value = volume.ToString()}
            };

            new TableObject(uuid, FileManager.PlayingSoundTableId, properties);
        }

        public static List<TableObject> GetAllPlayingSounds()
        {
            return Dav.Database.GetAllTableObjects(FileManager.PlayingSoundTableId, false);
        }

        public static void UpdatePlayingSound(Guid uuid, List<string> soundIds, string current, string repetitions, string randomly, string volume)
        {
            var playingSoundTableObject = Dav.Database.GetTableObject(uuid);

            if (playingSoundTableObject == null) return;
            if (playingSoundTableObject.TableId != FileManager.PlayingSoundTableId) return;

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
            if (ids.Count == 0) return "";

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

        #region Old Methods
        public static List<OldSoundDatabaseModel> GetAllSoundsFromDatabaseFile(StorageFile databaseFile)
        {
            List<OldSoundDatabaseModel> entries = new List<OldSoundDatabaseModel>();

            var db = new SQLiteConnection(databaseFile.Path);
            string selectCommandText = "SELECT * FROM Sound;";

            try
            {
                foreach (OldSoundDatabaseModel sound in db.Query<OldSoundDatabaseModel>(selectCommandText))
                    entries.Add(sound);
            }
            catch (SQLiteException error)
            {
                Debug.WriteLine(error.Message);
                return entries;
            }

            db.Close();
            return entries;
        }

        public static List<Category> GetAllCategoriesFromDatabaseFile(StorageFile databaseFile)
        {
            List<Category> entries = new List<Category>();
            var db = new SQLiteConnection(databaseFile.Path);

            string selectCommandText = "SELECT * FROM Category;";

            try
            {
                foreach (var category in db.Query<OldCategoryDatabaseModel>(selectCommandText))
                {
                    var categoryUuid = Guid.NewGuid();
                    Guid.TryParse(category.uuid, out categoryUuid);

                    entries.Add(new Category
                    {
                        Uuid = categoryUuid,
                        Name = category.name,
                        Icon = category.icon
                    });
                }
            }
            catch (SQLiteException error)
            {
                Debug.WriteLine(error.Message);
                return entries;
            }

            db.Close();
            return entries;
        }
        #endregion
    }
}

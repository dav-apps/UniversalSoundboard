using davClassLibrary;
using davClassLibrary.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UniversalSoundboard.Models;
using UniversalSoundBoard.Models;
using Windows.Storage;

namespace UniversalSoundBoard.DataAccess
{
    public class DatabaseOperations
    {
        #region General Methods
        public static async Task<TableObject> GetObjectAsync(Guid uuid)
        {
            return await Dav.Database.GetTableObjectAsync(uuid);
        }

        public static async Task<bool> ObjectExistsAsync(Guid uuid)
        {
            return await Dav.Database.TableObjectExistsAsync(uuid);
        }

        public static async Task DeleteObjectAsync(Guid uuid)
        {
            // Get the object and delete it
            var tableObject = await Dav.Database.GetTableObjectAsync(uuid);
            if (tableObject != null)
                await tableObject.DeleteAsync();
        }
        #endregion

        #region Sound
        public static async Task AddSoundAsync(Guid uuid, string name, string soundUuid, List<string> categoryUuids)
        {
            // Create TableObject with sound informations and TableObject with the Soundfile
            var properties = new List<Property>
            {
                new Property{ Name = FileManager.SoundTableNamePropertyName, Value = name },
                new Property{ Name = FileManager.SoundTableFavouritePropertyName, Value = bool.FalseString },
                new Property{ Name = FileManager.SoundTableSoundUuidPropertyName, Value = soundUuid }
            };

            if (categoryUuids != null)
                properties.Add(new Property { Name = FileManager.SoundTableCategoryUuidPropertyName, Value = string.Join(",", categoryUuids) });

            await TableObject.CreateAsync(uuid, FileManager.SoundTableId, properties);
        }

        public static async Task<List<TableObject>> GetAllSoundsAsync()
        {
            return await Dav.Database.GetAllTableObjectsAsync(FileManager.SoundTableId, false);
        }

        public static async Task UpdateSoundAsync(Guid uuid, string name, string favourite, string soundUuid, string imageUuid, List<string> categoryUuids)
        {
            // Get the sound table object
            var soundTableObject = await Dav.Database.GetTableObjectAsync(uuid);

            if (soundTableObject == null) return;
            if (soundTableObject.TableId != FileManager.SoundTableId) return;

            if (!string.IsNullOrEmpty(name))
                await soundTableObject.SetPropertyValueAsync(FileManager.SoundTableNamePropertyName, name);
            if (!string.IsNullOrEmpty(favourite))
                await soundTableObject.SetPropertyValueAsync(FileManager.SoundTableFavouritePropertyName, favourite);
            if (!string.IsNullOrEmpty(soundUuid))
                await soundTableObject.SetPropertyValueAsync(FileManager.SoundTableSoundUuidPropertyName, soundUuid);
            if (!string.IsNullOrEmpty(imageUuid))
                await soundTableObject.SetPropertyValueAsync(FileManager.SoundTableImageUuidPropertyName, imageUuid);
            if (categoryUuids != null)
                await soundTableObject.SetPropertyValueAsync(FileManager.SoundTableCategoryUuidPropertyName, string.Join(",", categoryUuids));
        }
        
        public static async Task DeleteSoundAsync(Guid uuid)
        {
            var soundTableObject = await Dav.Database.GetTableObjectAsync(uuid);

            if (soundTableObject == null) return;
            if (soundTableObject.TableId != FileManager.SoundTableId) return;

            // Delete the sound file and the image file
            Guid? soundFileUuid = FileManager.ConvertStringToGuid(soundTableObject.GetPropertyValue(FileManager.SoundTableSoundUuidPropertyName));
            Guid? imageFileUuid = FileManager.ConvertStringToGuid(soundTableObject.GetPropertyValue(FileManager.SoundTableImageUuidPropertyName));

            if (soundFileUuid.HasValue && !Equals(soundFileUuid, Guid.Empty))
            {
                var soundFileTableObject = await Dav.Database.GetTableObjectAsync(soundFileUuid.Value);
                if (soundFileTableObject != null)
                    await soundFileTableObject.DeleteAsync();
            }
            if (imageFileUuid.HasValue && !Equals(imageFileUuid, Guid.Empty))
            {
                var imageFileTableObject = await Dav.Database.GetTableObjectAsync(imageFileUuid.Value);
                if (imageFileTableObject != null)
                    await imageFileTableObject.DeleteAsync();
            }

            // Delete the sound itself
            await soundTableObject.DeleteAsync();
        }
        #endregion

        #region SoundFile
        public static async Task AddSoundFileAsync(Guid uuid, StorageFile audioFile)
        {
            await TableObject.CreateAsync(uuid, FileManager.SoundFileTableId, new FileInfo(audioFile.Path));
        }

        public static async Task<List<TableObject>> GetAllSoundFilesAsync()
        {
            return await Dav.Database.GetAllTableObjectsAsync(FileManager.SoundFileTableId, false);
        }
        #endregion SoundFile

        #region ImageFile
        public static async Task AddImageFileAsync(Guid uuid, StorageFile imageFile)
        {
            await TableObject.CreateAsync(uuid, FileManager.ImageFileTableId, new FileInfo(imageFile.Path));
        }

        public static async Task UpdateImageFileAsync(Guid uuid, StorageFile imageFile)
        {
            var imageFileTableObject = await Dav.Database.GetTableObjectAsync(uuid);

            if (imageFileTableObject == null) return;
            if (imageFileTableObject.TableId != FileManager.ImageFileTableId) return;

            await imageFileTableObject.SetFileAsync(new FileInfo(imageFile.Path));
        }
        #endregion

        #region Category
        public static async Task AddCategoryAsync(Guid uuid, string name, string icon)
        {
            List<Property> properties = new List<Property>
            {
                new Property{Name = FileManager.CategoryTableNamePropertyName, Value = name},
                new Property{Name = FileManager.CategoryTableIconPropertyName, Value = icon}
            };
            await TableObject.CreateAsync(uuid, FileManager.CategoryTableId, properties);
        }

        public static async Task<List<TableObject>> GetAllCategoriesAsync()
        {
            return await Dav.Database.GetAllTableObjectsAsync(FileManager.CategoryTableId, false);
        }

        public static async Task UpdateCategoryAsync(Guid uuid, string name, string icon)
        {
            var categoryTableObject = await Dav.Database.GetTableObjectAsync(uuid);

            if (categoryTableObject == null) return;
            if (categoryTableObject.TableId != FileManager.CategoryTableId) return;

            if (!string.IsNullOrEmpty(name))
                await categoryTableObject.SetPropertyValueAsync(FileManager.CategoryTableNamePropertyName, name);
            if (!string.IsNullOrEmpty(icon))
                await categoryTableObject.SetPropertyValueAsync(FileManager.CategoryTableIconPropertyName, icon);
        }
        #endregion

        #region PlayingSound
        public static async Task AddPlayingSoundAsync(Guid uuid, List<string> soundIds, int current, int repetitions, bool randomly, double volume)
        {
            var properties = new List<Property>
            {
                new Property{Name = FileManager.PlayingSoundTableSoundIdsPropertyName, Value = string.Join(",", soundIds)},
                new Property{Name = FileManager.PlayingSoundTableCurrentPropertyName, Value = current.ToString()},
                new Property{Name = FileManager.PlayingSoundTableRepetitionsPropertyName, Value = repetitions.ToString()},
                new Property{Name = FileManager.PlayingSoundTableRandomlyPropertyName, Value = randomly.ToString()},
                new Property{Name = FileManager.PlayingSoundTableVolumePropertyName, Value = volume.ToString()}
            };

            await TableObject.CreateAsync(uuid, FileManager.PlayingSoundTableId, properties);
        }

        public static async Task<List<TableObject>> GetAllPlayingSoundsAsync()
        {
            return await Dav.Database.GetAllTableObjectsAsync(FileManager.PlayingSoundTableId, false);
        }

        public static async Task UpdatePlayingSoundAsync(Guid uuid, List<string> soundIds, string current, string repetitions, string randomly, string volume)
        {
            var playingSoundTableObject = await Dav.Database.GetTableObjectAsync(uuid);

            if (playingSoundTableObject == null) return;
            if (playingSoundTableObject.TableId != FileManager.PlayingSoundTableId) return;

            if (soundIds != null)
                await playingSoundTableObject.SetPropertyValueAsync(FileManager.PlayingSoundTableSoundIdsPropertyName, string.Join(",", soundIds));
            if (!string.IsNullOrEmpty(current))
                await playingSoundTableObject.SetPropertyValueAsync(FileManager.PlayingSoundTableCurrentPropertyName, current);
            if (!string.IsNullOrEmpty(repetitions))
                await playingSoundTableObject.SetPropertyValueAsync(FileManager.PlayingSoundTableRepetitionsPropertyName, repetitions);
            if (!string.IsNullOrEmpty(randomly))
                await playingSoundTableObject.SetPropertyValueAsync(FileManager.PlayingSoundTableRandomlyPropertyName, randomly);
            if (!string.IsNullOrEmpty(volume))
                await playingSoundTableObject.SetPropertyValueAsync(FileManager.PlayingSoundTableVolumePropertyName, volume);
        }
        #endregion

        #region Order
        public static async Task<List<TableObject>> GetAllOrdersAsync()
        {
            return await Dav.Database.GetAllTableObjectsAsync(FileManager.OrderTableId, false);
        }
        #endregion

        #region CategoryOrder
        public static async Task SetCategoryOrderAsync(List<Guid> uuids)
        {
            // Check if the order already exists
            List<TableObject> tableObjects = await GetAllOrdersAsync();
            TableObject tableObject = tableObjects.Find(obj => obj.GetPropertyValue(FileManager.OrderTableTypePropertyName) == FileManager.CategoryOrderType);

            if (tableObject == null)
            {
                // Create a new table object
                List<Property> properties = new List<Property>
                {
                    // Set the type property
                    new Property { Name = FileManager.OrderTableTypePropertyName, Value = FileManager.CategoryOrderType }
                };

                int i = 0;
                foreach (var uuid in uuids)
                {
                    properties.Add(new Property { Name = i.ToString(), Value = uuid.ToString() });
                    i++;
                }

                await TableObject.CreateAsync(Guid.NewGuid(), FileManager.OrderTableId, properties);
            }
            else
            {
                // Update the existing object
                int i = 0;
                Dictionary<string, string> newProperties = new Dictionary<string, string>();
                foreach(var uuid in uuids)
                {
                    newProperties.Add(i.ToString(), uuid.ToString());
                    i++;
                }
                await tableObject.SetPropertyValuesAsync(newProperties);

                // Remove the properties that are outside of the uuids range
                List<string> removedProperties = new List<string>();
                foreach(var property in tableObject.Properties)
                    if (int.TryParse(property.Name, out int propertyIndex) && propertyIndex >= uuids.Count)
                        removedProperties.Add(property.Name);

                for (int j = 0; j < removedProperties.Count; j++)
                    await tableObject.RemovePropertyAsync(removedProperties[j]);
            }
        }
        #endregion

        #region SoundOrder
        public static async Task SetSoundOrderAsync(Guid categoryUuid, bool favourite, List<Guid> uuids)
        {
            // Check if the order object already exists
            List<TableObject> tableObjects = await GetAllOrdersAsync();
            TableObject tableObject = tableObjects.Find((TableObject obj) => {
                // Check if the object is of type Sound
                if (obj.GetPropertyValue(FileManager.OrderTableTypePropertyName) != FileManager.SoundOrderType) return false;

                // Check if the object has the right category uuid
                string categoryUuidString = obj.GetPropertyValue(FileManager.OrderTableCategoryPropertyName);
                Guid? cUuid = FileManager.ConvertStringToGuid(categoryUuidString);
                if (!cUuid.HasValue) return false;

                string favString = obj.GetPropertyValue(FileManager.OrderTableFavouritePropertyName);
                bool.TryParse(favString, out bool fav);

                return Equals(categoryUuid, cUuid) && favourite == fav;
            });

            if(tableObject == null)
            {
                // Create a new table object
                List<Property> properties = new List<Property>
                {
                    // Set the type property
                    new Property { Name = FileManager.OrderTableTypePropertyName, Value = FileManager.SoundOrderType },
                    // Set the category property
                    new Property { Name = FileManager.OrderTableCategoryPropertyName, Value = categoryUuid.ToString() },
                    // Set the favourite property
                    new Property { Name = FileManager.OrderTableFavouritePropertyName, Value = favourite.ToString() }
                };

                int i = 0;
                foreach (var uuid in uuids)
                {
                    properties.Add(new Property { Name = i.ToString(), Value = uuid.ToString() });
                    i++;
                }

                await TableObject.CreateAsync(Guid.NewGuid(), FileManager.OrderTableId, properties);
            }
            else
            {
                // Update the existing object
                int i = 0;
                Dictionary<string, string> newProperties = new Dictionary<string, string>();
                foreach (var uuid in uuids)
                {
                    newProperties.Add(i.ToString(), uuid.ToString());
                    i++;
                }
                await tableObject.SetPropertyValuesAsync(newProperties);
                
                bool removeNonExistentSounds = FileManager.itemViewHolder.User == null || !FileManager.itemViewHolder.User.IsLoggedIn ||
                                                (FileManager.itemViewHolder.User.IsLoggedIn && FileManager.syncFinished);

                if (removeNonExistentSounds)
                {
                    // Remove the properties that are outside of the uuids range
                    List<string> removedProperties = new List<string>();
                    foreach (var property in tableObject.Properties)
                        if (int.TryParse(property.Name, out int propertyIndex) && propertyIndex >= uuids.Count)
                            removedProperties.Add(property.Name);

                    for (int j = 0; j < removedProperties.Count; j++)
                        await tableObject.RemovePropertyAsync(removedProperties[j]);
                }
            }
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

        #region Export / Import
        public static async Task ExportDataAsync(StorageFolder exportFolder, IProgress<int> progress)
        {
            List<TableObjectData> tableObjectDataList = new List<TableObjectData>();
            var tableObjects = await Dav.Database.GetAllTableObjectsAsync(false);
            int i = 0;

            foreach(var tableObject in tableObjects)
            {
                tableObjectDataList.Add(tableObject.ToTableObjectData());

                if (tableObject.IsFile)
                {
                    // Create the folder for the table, if it does not exist
                    StorageFolder tableFolder;
                    if (await exportFolder.TryGetItemAsync(tableObject.TableId.ToString()) == null)
                        tableFolder = await exportFolder.CreateFolderAsync(tableObject.TableId.ToString());
                    else
                        tableFolder = await exportFolder.GetFolderAsync(tableObject.TableId.ToString());

                    StorageFile tableObjectFile = await StorageFile.GetFileFromPathAsync(tableObject.File.FullName);
                    if (tableObjectFile != null) await tableObjectFile.CopyAsync(tableFolder);
                }

                i++;
                progress.Report((int)Math.Round(100.0 / tableObjects.Count * i));
            }

            // Write the list of tableObjects as json
            StorageFile dataFile = await exportFolder.CreateFileAsync(Dav.ExportDataFileName, CreationCollisionOption.ReplaceExisting);
            await FileManager.WriteFileAsync(dataFile, tableObjectDataList);
        }

        public static async Task ImportDataAsync(StorageFolder importFolder, IProgress<int> progress)
        {
            StorageFile dataFile = await importFolder.GetFileAsync(Dav.ExportDataFileName);
            if (dataFile == null) return;

            List<TableObjectData> tableObjectDataList = await FileManager.GetTableObjectDataFromFile(dataFile);
            int i = 0;

            foreach(var tableObjectData in tableObjectDataList)
            {
                TableObject tableObject = TableObject.ConvertTableObjectDataToTableObject(tableObjectData);
                tableObject.UploadStatus = TableObject.TableObjectUploadStatus.New;

                if(!await Dav.Database.TableObjectExistsAsync(tableObject.Uuid))
                {
                    await Dav.Database.CreateTableObjectWithPropertiesAsync(tableObject);

                    if (tableObject.IsFile)
                    {
                        // Get the file from the appropriate folder
                        StorageFolder tableFolder = (StorageFolder)await importFolder.TryGetItemAsync(tableObject.TableId.ToString());
                        if (tableFolder == null) continue;

                        StorageFile tableObjectFile = (StorageFile)await tableFolder.TryGetItemAsync(tableObject.Uuid.ToString());
                        if (tableObjectFile == null) continue;

                        await tableObject.SetFileAsync(new FileInfo(tableObjectFile.Path));
                    }
                }

                i++;
                progress.Report((int)Math.Round(100.0 / tableObjectDataList.Count * i));
            }
        }
        #endregion
    }
}

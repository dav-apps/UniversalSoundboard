using davClassLibrary;
using davClassLibrary.Models;
using Sentry;
using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UniversalSoundboard.Common;
using UniversalSoundboard.Models;
using Windows.Storage;

namespace UniversalSoundboard.DataAccess
{
    public class DatabaseOperations
    {
        #region General Methods
        public static async Task<TableObject> GetTableObjectAsync(Guid uuid)
        {
            return await Dav.Database.GetTableObjectAsync(uuid);
        }

        public static async Task<List<TableObject>> GetTableObjectsByPropertyAsync(string propertyName, string propertyValue)
        {
            return await Dav.Database.GetTableObjectsByPropertyAsync(propertyName, propertyValue);
        }

        public static async Task<bool> TableObjectExistsAsync(Guid uuid)
        {
            return await Dav.Database.TableObjectExistsAsync(uuid);
        }

        public static async Task DeleteTableObjectAsync(Guid uuid)
        {
            // Get the object and delete it
            var tableObject = await Dav.Database.GetTableObjectAsync(uuid);
            if (tableObject != null)
                await tableObject.DeleteAsync();
        }
        #endregion

        #region Sound
        public static async Task<TableObject> CreateSoundAsync(
            Guid uuid,
            string name,
            bool favourite,
            Guid soundUuid,
            List<Guid> categoryUuids,
            string source
        )
        {
            // Create TableObject with sound informations and TableObject with the Soundfile
            var properties = new List<Property>
            {
                new Property{ Name = Constants.SoundTableNamePropertyName, Value = name },
                new Property{ Name = Constants.SoundTableFavouritePropertyName, Value = favourite.ToString() },
                new Property{ Name = Constants.SoundTableSoundUuidPropertyName, Value = soundUuid.ToString() }
            };

            if (categoryUuids != null)
                properties.Add(new Property { Name = Constants.SoundTableCategoryUuidPropertyName, Value = string.Join(",", categoryUuids) });

            if (source != null)
                properties.Add(new Property { Name = Constants.SoundTableSourcePropertyName, Value = source });

            return await TableObject.CreateAsync(uuid, Constants.SoundTableId, properties);
        }

        public static async Task<List<TableObject>> GetAllSoundsAsync()
        {
            return await Dav.Database.GetAllTableObjectsAsync(Constants.SoundTableId, false);
        }
        
        public static async Task DeleteSoundAsync(Guid uuid)
        {
            var soundTableObject = await Dav.Database.GetTableObjectAsync(uuid);
            if (soundTableObject == null || soundTableObject.TableId != Constants.SoundTableId) return;

            // Delete the sound file and the image file
            Guid? soundFileUuid = FileManager.ConvertStringToGuid(soundTableObject.GetPropertyValue(Constants.SoundTableSoundUuidPropertyName));
            Guid? imageFileUuid = FileManager.ConvertStringToGuid(soundTableObject.GetPropertyValue(Constants.SoundTableImageUuidPropertyName));

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
        public static async Task<TableObject> CreateSoundFileAsync(Guid uuid, StorageFile audioFile)
        {
            return await TableObject.CreateAsync(uuid, Constants.SoundFileTableId, new FileInfo(audioFile.Path));
        }
        #endregion

        #region ImageFile
        public static async Task<TableObject> CreateImageFileAsync(Guid uuid, StorageFile imageFile)
        {
            return await TableObject.CreateAsync(uuid, Constants.ImageFileTableId, new FileInfo(imageFile.Path));
        }

        public static async Task UpdateImageFileAsync(Guid uuid, StorageFile imageFile)
        {
            var imageFileTableObject = await Dav.Database.GetTableObjectAsync(uuid);
            if (imageFileTableObject == null || imageFileTableObject.TableId != Constants.ImageFileTableId) return;

            await imageFileTableObject.SetFileAsync(new FileInfo(imageFile.Path));
        }
        #endregion

        #region Category
        public static async Task<TableObject> CreateCategoryAsync(Guid uuid, Guid? parent, string name, string icon)
        {
            List<Property> properties = new List<Property>
            {
                new Property{ Name = Constants.CategoryTableNamePropertyName, Value = name },
                new Property{ Name = Constants.CategoryTableIconPropertyName, Value = icon }
            };

            if (parent.HasValue)
                properties.Add(new Property { Name = Constants.CategoryTableParentPropertyName, Value = parent.Value.ToString() });

            return await TableObject.CreateAsync(uuid, Constants.CategoryTableId, properties);
        }

        public static async Task<List<TableObject>> GetAllCategoriesAsync()
        {
            return await Dav.Database.GetAllTableObjectsAsync(Constants.CategoryTableId, false);
        }

        public static async Task UpdateCategoryAsync(Guid uuid, Guid? parent, string name, string icon)
        {
            var categoryTableObject = await Dav.Database.GetTableObjectAsync(uuid);
            if (categoryTableObject == null || categoryTableObject.TableId != Constants.CategoryTableId) return;

            if (parent.HasValue)
                await categoryTableObject.SetPropertyValueAsync(Constants.CategoryTableParentPropertyName, parent.Value.ToString());
            if (!string.IsNullOrEmpty(name))
                await categoryTableObject.SetPropertyValueAsync(Constants.CategoryTableNamePropertyName, name);
            if (!string.IsNullOrEmpty(icon))
                await categoryTableObject.SetPropertyValueAsync(Constants.CategoryTableIconPropertyName, icon);
        }

        public static async Task DeleteCategoryAsync(Guid uuid)
        {
            var categoryTableObject = await Dav.Database.GetTableObjectAsync(uuid);
            if (categoryTableObject == null || categoryTableObject.TableId != Constants.CategoryTableId) return;

            await categoryTableObject.DeleteAsync();
        }
        #endregion

        #region PlayingSound
        public static async Task<TableObject> CreatePlayingSoundAsync(Guid uuid, List<Guid> soundUuids, int current, int repetitions, bool randomly, int volume, bool muted)
        {
            var properties = new List<Property>
            {
                new Property{ Name = Constants.PlayingSoundTableSoundIdsPropertyName, Value = string.Join(",", soundUuids) },
                new Property{ Name = Constants.PlayingSoundTableCurrentPropertyName, Value = current.ToString() },
                new Property{ Name = Constants.PlayingSoundTableRepetitionsPropertyName, Value = repetitions.ToString() },
                new Property{ Name = Constants.PlayingSoundTableRandomlyPropertyName, Value = randomly.ToString() },
                new Property{ Name = Constants.PlayingSoundTableVolumePropertyName, Value = volume.ToString() },
                new Property{ Name = Constants.PlayingSoundTableMutedPropertyName, Value = muted.ToString() }
            };

            return await TableObject.CreateAsync(uuid, Constants.PlayingSoundTableId, properties);
        }

        public static async Task<List<TableObject>> GetAllPlayingSoundsAsync()
        {
            return await Dav.Database.GetAllTableObjectsAsync(Constants.PlayingSoundTableId, false);
        }

        public static async Task<TableObject> GetPlayingSoundAsync(Guid uuid)
        {
            var tableObject = await Dav.Database.GetTableObjectAsync(uuid);
            if (tableObject == null || tableObject.TableId != Constants.PlayingSoundTableId) return null;

            return tableObject;
        }
        #endregion

        #region Order
        public static async Task<List<TableObject>> GetAllOrdersAsync()
        {
            return await Dav.Database.GetAllTableObjectsAsync(Constants.OrderTableId, false);
        }
        #endregion

        #region CategoryOrder
        public static async Task SetCategoryOrderAsync(Guid parentCategoryUuid, List<Guid> uuids)
        {
            // Check if the order already exists
            List<TableObject> tableObjects = await GetAllOrdersAsync();
            bool rootCategory = parentCategoryUuid.Equals(Guid.Empty);

            TableObject tableObject = tableObjects.Find(obj =>
            {
                // Check if the object is of type Category
                if (obj.GetPropertyValue(Constants.OrderTableTypePropertyName) != Constants.CategoryOrderType) return false;

                // Check if the object has the correct parent category uuid
                string categoryUuidString = obj.GetPropertyValue(Constants.OrderTableCategoryPropertyName);
                Guid? cUuid = FileManager.ConvertStringToGuid(categoryUuidString);
                if (!cUuid.HasValue)
                {
                    // Return true if the object belongs to no category and the searched category is the root category
                    return rootCategory;
                }
                else return cUuid.Value.Equals(parentCategoryUuid);
            });

            if (tableObject == null)
            {
                // Create a new table object
                List<Property> properties = new List<Property>
                {
                    // Set the type property
                    new Property { Name = Constants.OrderTableTypePropertyName, Value = Constants.CategoryOrderType },
                    // Set the category property
                    new Property { Name = Constants.OrderTableCategoryPropertyName, Value = parentCategoryUuid.ToString() }
                };

                int i = 0;
                foreach (var uuid in uuids)
                {
                    properties.Add(new Property { Name = i.ToString(), Value = uuid.ToString() });
                    i++;
                }

                await TableObject.CreateAsync(Guid.NewGuid(), Constants.OrderTableId, properties);
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

        public static async Task DeleteCategoryOrderAsync(Guid parentCategoryUuid)
        {
            if (parentCategoryUuid.Equals(Guid.Empty)) return;

            // Find the table object
            List<TableObject> tableObjects = await GetAllOrdersAsync();
            TableObject tableObject = tableObjects.Find(obj =>
            {
                // Check if the object is of type Category
                if (obj.GetPropertyValue(Constants.OrderTableTypePropertyName) != Constants.CategoryOrderType) return false;

                string categoryUuidString = obj.GetPropertyValue(Constants.OrderTableCategoryPropertyName);
                Guid? cUuid = FileManager.ConvertStringToGuid(categoryUuidString);
                if (!cUuid.HasValue) return false;

                return cUuid.Value.Equals(parentCategoryUuid);
            });

            if (tableObject == null) return;

            // Delete the table object
            await tableObject.DeleteAsync();
        }
        #endregion

        #region SoundOrder
        public static async Task SetSoundOrderAsync(Guid categoryUuid, bool favourite, List<Guid> uuids)
        {
            // Check if the order object already exists
            List<TableObject> tableObjects = await GetAllOrdersAsync();
            TableObject tableObject = tableObjects.Find((TableObject obj) => {
                // Check if the object is of type Sound
                if (obj.GetPropertyValue(Constants.OrderTableTypePropertyName) != Constants.SoundOrderType) return false;

                // Check if the object has the right category uuid
                string categoryUuidString = obj.GetPropertyValue(Constants.OrderTableCategoryPropertyName);
                Guid? cUuid = FileManager.ConvertStringToGuid(categoryUuidString);
                if (!cUuid.HasValue) return false;

                string favString = obj.GetPropertyValue(Constants.OrderTableFavouritePropertyName);
                bool.TryParse(favString, out bool fav);

                return Equals(categoryUuid, cUuid) && favourite == fav;
            });

            if(tableObject == null)
            {
                // Create a new table object
                List<Property> properties = new List<Property>
                {
                    // Set the type property
                    new Property { Name = Constants.OrderTableTypePropertyName, Value = Constants.SoundOrderType },
                    // Set the category property
                    new Property { Name = Constants.OrderTableCategoryPropertyName, Value = categoryUuid.ToString() },
                    // Set the favourite property
                    new Property { Name = Constants.OrderTableFavouritePropertyName, Value = favourite.ToString() }
                };

                int i = 0;
                foreach (var uuid in uuids)
                {
                    properties.Add(new Property { Name = i.ToString(), Value = uuid.ToString() });
                    i++;
                }

                await TableObject.CreateAsync(Guid.NewGuid(), Constants.OrderTableId, properties);
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
                
                bool removeNonExistentSounds = !Dav.IsLoggedIn || (Dav.IsLoggedIn && FileManager.syncFinished);

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

        public static async Task DeleteSoundOrderAsync(Guid categoryUuid, bool favourite)
        {
            if (categoryUuid.Equals(Guid.Empty)) return;

            // Find the table object
            List<TableObject> tableObjects = await GetAllOrdersAsync();
            TableObject tableObject = tableObjects.Find((TableObject obj) => {
                // Check if the object is of type Sound
                if (obj.GetPropertyValue(Constants.OrderTableTypePropertyName) != Constants.SoundOrderType) return false;

                // Check if the object has the right category uuid
                string categoryUuidString = obj.GetPropertyValue(Constants.OrderTableCategoryPropertyName);
                Guid? cUuid = FileManager.ConvertStringToGuid(categoryUuidString);
                if (!cUuid.HasValue) return false;

                string favString = obj.GetPropertyValue(Constants.OrderTableFavouritePropertyName);
                bool.TryParse(favString, out bool fav);

                return Equals(categoryUuid, cUuid) && favourite == fav;
            });

            if (tableObject == null) return;

            // Delete the table object
            await tableObject.DeleteAsync();
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
            try
            {
                List<TableObjectData> tableObjectDataList = new List<TableObjectData>();
                var tableObjects = await Dav.Database.GetAllTableObjectsAsync(false);
                int i = 0;

                // Get the dav data folder
                StorageFolder davFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("dav");

                foreach (var tableObject in tableObjects)
                {
                    try
                    {
                        tableObjectDataList.Add(tableObject.ToTableObjectData());

                        if (tableObject.IsFile && tableObject.File != null)
                        {
                            // Create the folder for the table, if it does not exist
                            StorageFolder tableFolder;
                            if (await exportFolder.TryGetItemAsync(tableObject.TableId.ToString()) == null)
                                tableFolder = await exportFolder.CreateFolderAsync(tableObject.TableId.ToString());
                            else
                                tableFolder = await exportFolder.GetFolderAsync(tableObject.TableId.ToString());

                            // Get the table folder within the dav folder
                            StorageFolder davTableFolder = (StorageFolder)await davFolder.TryGetItemAsync(tableObject.TableId.ToString());
                            if (davTableFolder == null) continue;

                            // Get the table object file within the table folder
                            StorageFile tableObjectFile = (StorageFile)await davTableFolder.TryGetItemAsync(tableObject.File.Name);
                            if (tableObjectFile == null) continue;

                            await tableObjectFile.CopyAsync(tableFolder);
                        }
                    }
                    catch (Exception innerException)
                    {
                        SentrySdk.CaptureException(innerException);
                    }

                    i++;
                    progress.Report((int)Math.Round(100.0 / tableObjects.Count * i));
                }

                // Write the list of tableObjects as json
                StorageFile dataFile = await exportFolder.CreateFileAsync(Constants.ExportDataFileName, CreationCollisionOption.ReplaceExisting);
                await FileManager.WriteFileAsync(dataFile, tableObjectDataList);
            }
            catch (Exception outerException)
            {
                SentrySdk.CaptureException(outerException);
            }
        }

        public static async Task ImportDataAsync(StorageFolder importFolder, IProgress<int> progress)
        {
            try
            {
                StorageFile dataFile = await importFolder.GetFileAsync(Constants.ExportDataFileName);
                if (dataFile == null) return;

                List<TableObjectData> tableObjectDataList = await FileManager.GetTableObjectDataFromFile(dataFile);
                int i = 0;

                foreach (var tableObjectData in tableObjectDataList)
                {
                    try
                    {
                        TableObject tableObject = tableObjectData.ToTableObject();
                        tableObject.UploadStatus = TableObjectUploadStatus.New;

                        if (!await Dav.Database.TableObjectExistsAsync(tableObject.Uuid))
                        {
                            if (tableObject.IsFile)
                            {
                                // Get the file from the appropriate folder
                                StorageFolder tableFolder = (StorageFolder)await importFolder.TryGetItemAsync(tableObject.TableId.ToString());
                                if (tableFolder == null) continue;

                                StorageFile tableObjectFile = (StorageFile)await tableFolder.TryGetItemAsync(tableObject.Uuid.ToString());
                                if (tableObjectFile == null) continue;

                                await Dav.Database.CreateTableObjectWithPropertiesAsync(tableObject);
                                await tableObject.SetFileAsync(new FileInfo(tableObjectFile.Path));
                            }
                            else
                            {
                                await Dav.Database.CreateTableObjectWithPropertiesAsync(tableObject);
                            }
                        }
                    }
                    catch (Exception innerException)
                    {
                        SentrySdk.CaptureException(innerException);
                    }

                    i++;
                    progress.Report((int)Math.Round(100.0 / tableObjectDataList.Count * i));
                }
            }
            catch (Exception outerException)
            {
                SentrySdk.CaptureException(outerException);
            }
        }
        #endregion
    }
}

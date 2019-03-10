using davClassLibrary;
using davClassLibrary.Common;
using davClassLibrary.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UniversalSoundboard.Tests.Common;
using UniversalSoundBoard.DataAccess;

namespace UniversalSoundboard.Tests.DataAccess
{
    [TestClass][DoNotParallelize]
    public class DatabaseOperationsTest
    {
        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            ProjectInterface.RetrieveConstants = new RetrieveConstants();
            ProjectInterface.GeneralMethods = new GeneralMethods();
            ProjectInterface.LocalDataSettings = new LocalDataSettings();
            ProjectInterface.TriggerAction = new TriggerAction();
        }

        [TestInitialize]
        public async Task TestInit()
        {
            // Delete all files and folders in the test folder except the database file
            var davFolder = new DirectoryInfo(FileManager.GetDavDataPath());
            foreach (var folder in davFolder.GetDirectories())
                folder.Delete(true);
            
            // Clear the database
            var database = new davClassLibrary.DataAccess.DavDatabase();
            await database.DropAsync();
        }

        #region GetObject
        [TestMethod]
        public async Task GetObjectShouldReturnTheObject()
        {
            // Arrange
            int tableId = 23;
            var tableObject = await TableObject.CreateAsync(tableId);

            // Act
            var tableObjectFromDatabase = await DatabaseOperations.GetObjectAsync(tableObject.Uuid);

            // Assert
            Assert.IsNotNull(tableObjectFromDatabase);
            Assert.AreEqual(tableId, tableObjectFromDatabase.TableId);
        }

        [TestMethod]
        public async Task GetObjectShouldReturnNullIfTheObjectDoesNotExist()
        {
            // Arrange
            var uuid = Guid.NewGuid();

            // Act
            var tableObjectFromDatabase = await DatabaseOperations.GetObjectAsync(uuid);

            // Assert
            Assert.IsNull(tableObjectFromDatabase);
        }
        #endregion

        #region ObjectExists
        [TestMethod]
        public async Task ObjectExistsShouldReturnTrueIfTheObjectExists()
        {
            // Arrange
            int tableId = 34;
            var tableObject = await TableObject.CreateAsync(tableId);

            // Act
            bool exists = await DatabaseOperations.ObjectExistsAsync(tableObject.Uuid);

            // Assert
            Assert.IsTrue(exists);
        }

        [TestMethod]
        public async Task ObjectExistsShouldReturnFalseIfTheObjectDoesNotExist()
        {
            // Arrange
            var uuid = Guid.NewGuid();

            // Act
            bool exists = await DatabaseOperations.ObjectExistsAsync(uuid);

            // Assert
            Assert.IsFalse(exists);
        }
        #endregion

        #region DeleteObject
        [TestMethod]
        public async Task DeleteObjectShouldDeleteTheObjectImmediatelyIfTheUserIsNotLoggedIn()
        {
            // Arrange
            int tableId = 21;
            var tableObject = await TableObject.CreateAsync(tableId);

            // Act
            await DatabaseOperations.DeleteObjectAsync(tableObject.Uuid);

            // Assert
            var tableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(tableObject.Uuid);
            Assert.IsNull(tableObjectFromDatabase);
        }

        [TestMethod]
        public async Task DeleteObjectShouldSetTheUploadStatusToDeletedIfTheUserIsLoggedIn()
        {
            // Arrange
            ProjectInterface.LocalDataSettings.SetValue(Dav.jwtKey, Constants.Jwt);
            int tableId = 21;
            var tableObject = await TableObject.CreateAsync(tableId);

            // Act
            await DatabaseOperations.DeleteObjectAsync(tableObject.Uuid);

            // Assert
            var tableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(tableObject.Uuid);
            Assert.IsNotNull(tableObjectFromDatabase);
            Assert.AreEqual(TableObject.TableObjectUploadStatus.Deleted, tableObjectFromDatabase.UploadStatus);
        }
        #endregion

        #region AddSound
        [TestMethod]
        public async Task AddSoundShouldCreateTheSoundObjectWithTheCorrectProperties()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            string soundFileUuid = Guid.NewGuid().ToString();
            List<string> categoryUuids = new List<string>
            {
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString()
            };
            string name = "Phoenix Objection";

            // Act
            await DatabaseOperations.AddSoundAsync(uuid, name, soundFileUuid, categoryUuids);

            // Assert
            var tableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(uuid);
            Assert.AreEqual(FileManager.SoundTableId, tableObjectFromDatabase.TableId);
            Assert.AreEqual(Guid.Parse(soundFileUuid), Guid.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableSoundUuidPropertyName)));
            Assert.AreEqual(name, tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableNamePropertyName));
            Assert.AreEqual(false, bool.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableFavouritePropertyName)));

            int i = 0;
            string[] tableObjectCategoryUuids = tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableCategoryUuidPropertyName).Split(",");
            foreach(var categoryUuid in categoryUuids)
            {
                Assert.AreEqual(Guid.Parse(categoryUuid), Guid.Parse(tableObjectCategoryUuids[i]));
                i++;
            }
        }

        [TestMethod]
        public async Task AddSoundShouldCreateTheSoundObjectWithoutCategories()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            string soundFileUuid = Guid.NewGuid().ToString();
            string name = "Godot Objection";

            // Act
            await DatabaseOperations.AddSoundAsync(uuid, name, soundFileUuid, null);

            // Assert
            var tableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(uuid);
            Assert.AreEqual(FileManager.SoundTableId, tableObjectFromDatabase.TableId);
            Assert.AreEqual(Guid.Parse(soundFileUuid), Guid.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableSoundUuidPropertyName)));
            Assert.AreEqual(name, tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableNamePropertyName));
            Assert.AreEqual(false, bool.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableFavouritePropertyName)));
            Assert.IsNull(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableCategoryUuidPropertyName));
        }
        #endregion

        #region GetAllSounds
        [TestMethod]
        public async Task GetAllSoundShouldReturnAllSounds()
        {
            // Arrange
            var firstSoundUuid = Guid.NewGuid();
            var secondSoundUuid = Guid.NewGuid();
            var firstSoundFileUuid = Guid.NewGuid();
            var secondSoundFileUuid = Guid.NewGuid();
            string firstSoundName = "Phoenix Objection";
            string secondSoundName = "Godot Hold it";

            List<string> firstSoundCategoryUuids = new List<string>
            {
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString()
            };
            List<string> secondSoundCategoryUuids = new List<string>
            {
                Guid.NewGuid().ToString()
            };

            // Create the sounds
            await DatabaseOperations.AddSoundAsync(firstSoundUuid, firstSoundName, firstSoundFileUuid.ToString(), firstSoundCategoryUuids);
            await DatabaseOperations.AddSoundAsync(secondSoundUuid, secondSoundName, secondSoundFileUuid.ToString(), secondSoundCategoryUuids);

            // Act
            List<TableObject> sounds = await DatabaseOperations.GetAllSoundsAsync();

            // Assert
            Assert.AreEqual(2, sounds.Count);

            // Test the first table object
            Assert.AreEqual(firstSoundUuid, sounds[0].Uuid);
            Assert.AreEqual(firstSoundFileUuid, Guid.Parse(sounds[0].GetPropertyValue(FileManager.SoundTableSoundUuidPropertyName)));
            Assert.AreEqual(firstSoundName, sounds[0].GetPropertyValue(FileManager.SoundTableNamePropertyName));

            int i = 0;
            string[] firstTableObjectCategoryUuids = sounds[0].GetPropertyValue(FileManager.SoundTableCategoryUuidPropertyName).Split(",");
            foreach (var categoryUuid in firstSoundCategoryUuids)
            {
                Assert.AreEqual(Guid.Parse(categoryUuid), Guid.Parse(firstTableObjectCategoryUuids[i]));
                i++;
            }

            // Test the second table object
            Assert.AreEqual(secondSoundUuid, sounds[1].Uuid);
            Assert.AreEqual(secondSoundFileUuid, Guid.Parse(sounds[1].GetPropertyValue(FileManager.SoundTableSoundUuidPropertyName)));
            Assert.AreEqual(secondSoundName, sounds[1].GetPropertyValue(FileManager.SoundTableNamePropertyName));

            i = 0;
            string[] secondTableObjectCategoryUuids = sounds[1].GetPropertyValue(FileManager.SoundTableCategoryUuidPropertyName).Split(",");
            foreach (var categoryUuid in secondSoundCategoryUuids)
            {
                Assert.AreEqual(Guid.Parse(categoryUuid), Guid.Parse(secondTableObjectCategoryUuids[i]));
                i++;
            }
        }

        [TestMethod]
        public async Task GetAllSoundsShouldReturnEmptyListIfThereAreNoSounds()
        {
            // Act
            List<TableObject> sounds = await DatabaseOperations.GetAllSoundsAsync();

            // Assert
            Assert.AreEqual(0, sounds.Count);
        }
        #endregion

        #region UpdateSound
        [TestMethod]
        public async Task UpdateSoundShouldUpdateAllValuesOfTheSound()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            var oldSoundFileUuid = Guid.NewGuid();
            var newSoundFileUuid = Guid.NewGuid();
            string oldName = "Phoenix Objection";
            string newName = "Godot Objection";
            var imageFileUuid = Guid.NewGuid();
            bool newFavourite = true;
            List<string> oldCategoryUuids = new List<string>
            {
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString()
            };
            List<string> newCategoryUuids = new List<string>
            {
                Guid.NewGuid().ToString()
            };

            // Create the sound
            await DatabaseOperations.AddSoundAsync(uuid, oldName, oldSoundFileUuid.ToString(), oldCategoryUuids);

            // Act
            await DatabaseOperations.UpdateSoundAsync(uuid, newName, newFavourite.ToString(), newSoundFileUuid.ToString(), imageFileUuid.ToString(), newCategoryUuids);

            // Assert
            var tableObjectFromDatabase = await DatabaseOperations.GetObjectAsync(uuid);
            Assert.AreEqual(newSoundFileUuid, Guid.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableSoundUuidPropertyName)));
            Assert.AreEqual(newName, tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableNamePropertyName));
            Assert.AreEqual(imageFileUuid, Guid.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableImageUuidPropertyName)));
            Assert.AreEqual(newFavourite, bool.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableFavouritePropertyName)));
            Assert.AreEqual(newCategoryUuids[0], tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableCategoryUuidPropertyName));
        }

        [TestMethod]
        public async Task UpdateSoundShouldUpdateTheNameOfTheSound()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            string oldName = "Phoenix Hold it";
            string newName = "Godot Objection";
            var soundFileUuid = Guid.NewGuid();

            // Create the sound
            await DatabaseOperations.AddSoundAsync(uuid, oldName, soundFileUuid.ToString(), null);

            // Act
            await DatabaseOperations.UpdateSoundAsync(uuid, newName, null, null, null, null);

            // Assert
            var tableObjectFromDatabase = await DatabaseOperations.GetObjectAsync(uuid);
            Assert.AreEqual(newName, tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableNamePropertyName));
            Assert.AreEqual(soundFileUuid, Guid.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableSoundUuidPropertyName)));
            Assert.IsFalse(bool.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableFavouritePropertyName)));
            Assert.IsNull(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableImageUuidPropertyName));
            Assert.IsNull(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableCategoryUuidPropertyName));
        }

        [TestMethod]
        public async Task UpdateSoundShouldUpdateTheFavouriteOfTheSound()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            string name = "Phoenix Objection";
            bool newFavourite = true;
            var soundFileUuid = Guid.NewGuid();

            // Create the sound
            await DatabaseOperations.AddSoundAsync(uuid, name, soundFileUuid.ToString(), null);

            // Act
            await DatabaseOperations.UpdateSoundAsync(uuid, null, newFavourite.ToString(), null, null, null);

            // Assert
            var tableObjectFromDatabase = await DatabaseOperations.GetObjectAsync(uuid);
            Assert.AreEqual(name, tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableNamePropertyName));
            Assert.AreEqual(soundFileUuid, Guid.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableSoundUuidPropertyName)));
            Assert.AreEqual(newFavourite, bool.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableFavouritePropertyName)));
            Assert.IsNull(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableImageUuidPropertyName));
            Assert.IsNull(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableCategoryUuidPropertyName));
        }

        [TestMethod]
        public async Task UpdateSoundShouldUpdateTheSoundFileUuidOfTheSound()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            string name = "Phoenix Objection";
            var oldSoundFileUuid = Guid.NewGuid();
            var newSoundFileUuid = Guid.NewGuid();

            // Create the sound
            await DatabaseOperations.AddSoundAsync(uuid, name, oldSoundFileUuid.ToString(), null);

            // Act
            await DatabaseOperations.UpdateSoundAsync(uuid, null,null, newSoundFileUuid.ToString(), null, null);

            // Assert
            var tableObjectFromDatabase = await DatabaseOperations.GetObjectAsync(uuid);
            Assert.AreEqual(name, tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableNamePropertyName));
            Assert.AreEqual(newSoundFileUuid, Guid.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableSoundUuidPropertyName)));
            Assert.IsFalse(bool.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableFavouritePropertyName)));
            Assert.IsNull(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableImageUuidPropertyName));
            Assert.IsNull(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableCategoryUuidPropertyName));
        }

        [TestMethod]
        public async Task UpdateSoundShouldUpdateTheImageUuidOfTheSound()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            string name = "Phoenix Objection";
            var soundFileUuid = Guid.NewGuid();
            var newImageUuid = Guid.NewGuid();

            // Create the sound
            await DatabaseOperations.AddSoundAsync(uuid, name, soundFileUuid.ToString(), null);

            // Act
            await DatabaseOperations.UpdateSoundAsync(uuid, null, null, null, newImageUuid.ToString(), null);

            // Assert
            var tableObjectFromDatabase = await DatabaseOperations.GetObjectAsync(uuid);
            Assert.AreEqual(name, tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableNamePropertyName));
            Assert.AreEqual(soundFileUuid, Guid.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableSoundUuidPropertyName)));
            Assert.IsFalse(bool.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableFavouritePropertyName)));
            Assert.AreEqual(newImageUuid, Guid.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableImageUuidPropertyName)));
            Assert.IsNull(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableCategoryUuidPropertyName));
        }

        [TestMethod]
        public async Task UpdateSoundShouldUpdateTheCategoryUuidsOfTheSound()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            string name = "Phoenix Objection";
            var soundFileUuid = Guid.NewGuid();
            List<string> oldCategories = new List<string>
            {
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString()
            };
            List<string> newCategories = new List<string>
            {
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString()
            };

            // Create the sound
            await DatabaseOperations.AddSoundAsync(uuid, name, soundFileUuid.ToString(), oldCategories);

            // Act
            await DatabaseOperations.UpdateSoundAsync(uuid, null, null, null, null, newCategories);

            // Assert
            var tableObjectFromDatabase = await DatabaseOperations.GetObjectAsync(uuid);
            Assert.AreEqual(name, tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableNamePropertyName));
            Assert.AreEqual(soundFileUuid, Guid.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableSoundUuidPropertyName)));
            Assert.IsFalse(bool.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableFavouritePropertyName)));
            Assert.IsNull(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableImageUuidPropertyName));

            int i = 0;
            string[] categoryUuids = tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableCategoryUuidPropertyName).Split(",");
            foreach (var categoryUuid in newCategories)
            {
                Assert.AreEqual(Guid.Parse(categoryUuid), Guid.Parse(categoryUuids[i]));
                i++;
            }
        }
        #endregion

        #region DeleteSound
        [TestMethod]
        public async Task DeleteSoundShouldDeleteTheSound()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            string name = "Phoenix Objection";

            // Create the sound
            await DatabaseOperations.AddSoundAsync(uuid, name, Guid.NewGuid().ToString(), null);

            // Act
            await DatabaseOperations.DeleteSoundAsync(uuid);

            // Assert
            var tableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(uuid);
            Assert.IsNull(tableObjectFromDatabase);
        }

        [TestMethod]
        public async Task DeleteSoundShouldDeleteTheSoundAndTheSoundFile()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            var soundFileUuid = Guid.NewGuid();
            string name = "Phoenix Objection";
            List<string> categoryUuids = new List<string>
            {
                Guid.NewGuid().ToString()
            };

            // Create the sound
            await DatabaseOperations.AddSoundAsync(uuid, name, soundFileUuid.ToString(), categoryUuids);

            // Create the sound file table object
            await TableObject.CreateAsync(soundFileUuid, FileManager.SoundFileTableId);

            // Act
            await DatabaseOperations.DeleteSoundAsync(uuid);

            // Assert
            var tableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(uuid);
            Assert.IsNull(tableObjectFromDatabase);

            var soundFileTableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(soundFileUuid);
            Assert.IsNull(soundFileTableObjectFromDatabase);
        }

        [TestMethod]
        public async Task DeleteSoundShouldDeleteTheSoundTheSoundFileAndTheImageFile()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            var soundFileUuid = Guid.NewGuid();
            var imageFileUuid = Guid.NewGuid();
            string name = "Phoenix Objection";

            // Create the sound
            await DatabaseOperations.AddSoundAsync(uuid, name, soundFileUuid.ToString(), null);
            await DatabaseOperations.UpdateSoundAsync(uuid, null, null, null, imageFileUuid.ToString(), null);

            // Create the sound file table object
            await TableObject.CreateAsync(soundFileUuid, FileManager.SoundFileTableId);

            // Create the image file table object
            await TableObject.CreateAsync(imageFileUuid, FileManager.ImageFileTableId);

            // Act
            await DatabaseOperations.DeleteSoundAsync(uuid);

            // Assert
            var soundFromDatabase = await Dav.Database.GetTableObjectAsync(uuid);
            Assert.IsNull(soundFromDatabase);

            var soundFileFromDatabase = await Dav.Database.GetTableObjectAsync(soundFileUuid);
            Assert.IsNull(soundFileFromDatabase);

            var imageFileFromDatabase = await Dav.Database.GetTableObjectAsync(imageFileUuid);
            Assert.IsNull(imageFileFromDatabase);
        }
        #endregion

        #region AddCategory
        [TestMethod]
        public async Task AddCategoryShouldCreateTheCategoryObjectWithTheCorrectProperties()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            string name = "Testcategory";
            string icon = "icon";

            // Act
            await DatabaseOperations.AddCategoryAsync(uuid, name, icon);

            // Assert
            var tableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(uuid);
            Assert.AreEqual(FileManager.CategoryTableId, tableObjectFromDatabase.TableId);
            Assert.AreEqual(name, tableObjectFromDatabase.GetPropertyValue(FileManager.CategoryTableNamePropertyName));
            Assert.AreEqual(icon, tableObjectFromDatabase.GetPropertyValue(FileManager.CategoryTableIconPropertyName));
        }
        #endregion

        #region GetAllCategories
        [TestMethod]
        public async Task GetAllCategoriesShouldReturnAllCategories()
        {
            // Arrange
            var firstUuid = Guid.NewGuid();
            var secondUuid = Guid.NewGuid();
            string firstName = "FirstName";
            string secondName = "SecondName";
            string firstIcon = "FirstIcon";
            string secondIcon = "SecondIcon";

            // Create the categories
            await DatabaseOperations.AddCategoryAsync(firstUuid, firstName, firstIcon);
            await DatabaseOperations.AddCategoryAsync(secondUuid, secondName, secondIcon);

            // Act
            List<TableObject> categories = await DatabaseOperations.GetAllCategoriesAsync();

            // Assert
            Assert.AreEqual(2, categories.Count);

            Assert.AreEqual(firstUuid, categories[0].Uuid);
            Assert.AreEqual(firstName, categories[0].GetPropertyValue(FileManager.CategoryTableNamePropertyName));
            Assert.AreEqual(firstIcon, categories[0].GetPropertyValue(FileManager.CategoryTableIconPropertyName));

            Assert.AreEqual(secondUuid, categories[1].Uuid);
            Assert.AreEqual(secondName, categories[1].GetPropertyValue(FileManager.CategoryTableNamePropertyName));
            Assert.AreEqual(secondIcon, categories[1].GetPropertyValue(FileManager.CategoryTableIconPropertyName));
        }

        [TestMethod]
        public async Task GetAllCategoriesShouldReturnEmptyListIfThereAreNoCategories()
        {
            // Act
            List<TableObject> categories = await DatabaseOperations.GetAllCategoriesAsync();

            // Assert
            Assert.AreEqual(0, categories.Count);
        }
        #endregion

        #region UpdateCategory
        [TestMethod]
        public async Task UpdateCategoryShouldUpdateAllValuesOfTheCategory()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            var oldName = "TestCategory";
            var newName = "UpdatedCategory";
            var oldIcon = "icon";
            var newIcon = "updatedIcon";

            // Create the category
            await DatabaseOperations.AddCategoryAsync(uuid, oldName, oldIcon);

            // Act
            await DatabaseOperations.UpdateCategoryAsync(uuid, newName, newIcon);

            // Assert
            var tableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(uuid);
            Assert.AreEqual(newName, tableObjectFromDatabase.GetPropertyValue(FileManager.CategoryTableNamePropertyName));
            Assert.AreEqual(newIcon, tableObjectFromDatabase.GetPropertyValue(FileManager.CategoryTableIconPropertyName));
        }

        [TestMethod]
        public async Task UpdateCategoryShouldUpdateTheNameOfTheCategory()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            var oldName = "Test category";
            var newName = "Updated category";
            var icon = "icon";

            // Create the category
            await DatabaseOperations.AddCategoryAsync(uuid, oldName, icon);

            // Act
            await DatabaseOperations.UpdateCategoryAsync(uuid, newName, null);

            // Assert
            var tableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(uuid);
            Assert.AreEqual(newName, tableObjectFromDatabase.GetPropertyValue(FileManager.CategoryTableNamePropertyName));
            Assert.AreEqual(icon, tableObjectFromDatabase.GetPropertyValue(FileManager.CategoryTableIconPropertyName));
        }

        [TestMethod]
        public async Task UpdateCategoryShouldUpdateTheIconOfTheCategory()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            var name = "TestCategory";
            var oldIcon = "icon";
            var newIcon = "updated icon";

            // Create the category
            await DatabaseOperations.AddCategoryAsync(uuid, name, oldIcon);

            // Act
            await DatabaseOperations.UpdateCategoryAsync(uuid, null, newIcon);

            // Assert
            var tableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(uuid);
            Assert.AreEqual(name, tableObjectFromDatabase.GetPropertyValue(FileManager.CategoryTableNamePropertyName));
            Assert.AreEqual(newIcon, tableObjectFromDatabase.GetPropertyValue(FileManager.CategoryTableIconPropertyName));
        }
        #endregion

        #region AddPlayingSound
        [TestMethod]
        public async Task AddPlayingSoundShouldCreateThePlayingSoundObjectWithTheCorrectProperties()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            List<string> soundUuids = new List<string>
            {
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString()
            };
            int current = 1;
            int repetitions = 3;
            bool randomly = true;
            double volume = 0.4;

            // Act
            await DatabaseOperations.AddPlayingSoundAsync(uuid, soundUuids, current, repetitions, randomly, volume);

            // Assert
            var tableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(uuid);
            Assert.AreEqual(FileManager.PlayingSoundTableId, tableObjectFromDatabase.TableId);
            Assert.AreEqual(current, int.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableCurrentPropertyName)));
            Assert.AreEqual(repetitions, int.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableRepetitionsPropertyName)));
            Assert.AreEqual(randomly, bool.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableRandomlyPropertyName)));
            Assert.AreEqual(volume, double.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableVolumePropertyName)));

            int i = 0;
            string[] tableObjectSoundUuids = tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableSoundIdsPropertyName).Split(",");
            foreach(var soundUuid in soundUuids)
            {
                Assert.AreEqual(soundUuid, tableObjectSoundUuids[i]);
                i++;
            }
        }
        #endregion

        #region GetAllPlayingSounds
        [TestMethod]
        public async Task GetAllPlayingSoundsShouldReturnAllPlayingSounds()
        {
            // Arrange
            var firstUuid = Guid.NewGuid();
            var secondUuid = Guid.NewGuid();
            int firstCurrent = 0;
            int secondCurrent = 1;
            int firstRepetitions = 3;
            int secondRepetitions = 1;
            bool firstRandomly = true;
            bool secondRandomly = false;
            double firstVolume = 0.6;
            double secondVolume = 0.2;

            List<string> firstSoundUuids = new List<string>
            {
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString()
            };
            List<string> secondSoundUuids = new List<string>
            {
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString()
            };

            // Create the playing sounds
            await DatabaseOperations.AddPlayingSoundAsync(firstUuid, firstSoundUuids, firstCurrent, firstRepetitions, firstRandomly, firstVolume);
            await DatabaseOperations.AddPlayingSoundAsync(secondUuid, secondSoundUuids, secondCurrent, secondRepetitions, secondRandomly, secondVolume);

            // Act
            List<TableObject> playingSounds = await DatabaseOperations.GetAllPlayingSoundsAsync();

            // Assert
            Assert.AreEqual(2, playingSounds.Count);

            Assert.AreEqual(firstCurrent, int.Parse(playingSounds[0].GetPropertyValue(FileManager.PlayingSoundTableCurrentPropertyName)));
            Assert.AreEqual(firstRepetitions, int.Parse(playingSounds[0].GetPropertyValue(FileManager.PlayingSoundTableRepetitionsPropertyName)));
            Assert.AreEqual(firstRandomly, bool.Parse(playingSounds[0].GetPropertyValue(FileManager.PlayingSoundTableRandomlyPropertyName)));
            Assert.AreEqual(firstVolume, double.Parse(playingSounds[0].GetPropertyValue(FileManager.PlayingSoundTableVolumePropertyName)));

            int i = 0;
            string[] firstTableObjectSoundUuids = playingSounds[0].GetPropertyValue(FileManager.PlayingSoundTableSoundIdsPropertyName).Split(",");
            foreach(var soundUuid in firstSoundUuids)
            {
                Assert.AreEqual(soundUuid, firstTableObjectSoundUuids[i]);
                i++;
            }

            Assert.AreEqual(secondCurrent, int.Parse(playingSounds[1].GetPropertyValue(FileManager.PlayingSoundTableCurrentPropertyName)));
            Assert.AreEqual(secondRepetitions, int.Parse(playingSounds[1].GetPropertyValue(FileManager.PlayingSoundTableRepetitionsPropertyName)));
            Assert.AreEqual(secondRandomly, bool.Parse(playingSounds[1].GetPropertyValue(FileManager.PlayingSoundTableRandomlyPropertyName)));
            Assert.AreEqual(secondVolume, double.Parse(playingSounds[1].GetPropertyValue(FileManager.PlayingSoundTableVolumePropertyName)));

            i = 0;
            string[] secondTableObjectSoundUuids = playingSounds[1].GetPropertyValue(FileManager.PlayingSoundTableSoundIdsPropertyName).Split(",");
            foreach (var soundUuid in secondSoundUuids)
            {
                Assert.AreEqual(soundUuid, secondTableObjectSoundUuids[i]);
                i++;
            }
        }

        [TestMethod]
        public async Task GetAllPlayingSoundsShouldReturnEmptyListIfThereAreNoPlayingSounds()
        {
            // Act
            List<TableObject> playingSounds = await DatabaseOperations.GetAllPlayingSoundsAsync();

            // Assert
            Assert.AreEqual(0, playingSounds.Count);
        }
        #endregion

        #region UpdatePlayingSound
        [TestMethod]
        public async Task UpdatePlayingSoundShouldUpdateAllValuesOfThePlayingSound()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            List<string> oldSoundUuids = new List<string>
            {
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString()
            };
            List<string> newSoundUuids = new List<string>
            {
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString()
            };
            int oldCurrent = 0;
            int newCurrent = 1;
            int oldRepetitions = 3;
            int newRepetitions = 5;
            bool oldRandomly = true;
            bool newRandomly = false;
            double oldVolume = 0.4;
            double newVolume = 0.96;

            // Create the playing sound
            await DatabaseOperations.AddPlayingSoundAsync(uuid, oldSoundUuids, oldCurrent, oldRepetitions, oldRandomly, oldVolume);

            // Act
            await DatabaseOperations.UpdatePlayingSoundAsync(uuid, newSoundUuids, newCurrent.ToString(), newRepetitions.ToString(), newRandomly.ToString(), newVolume.ToString());

            // Assert
            var tableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(uuid);
            Assert.AreEqual(newCurrent, int.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableCurrentPropertyName)));
            Assert.AreEqual(newRepetitions, int.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableRepetitionsPropertyName)));
            Assert.AreEqual(newRandomly, bool.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableRandomlyPropertyName)));
            Assert.AreEqual(newVolume, double.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableVolumePropertyName)));

            int i = 0;
            string[] tableObjectSoundUuids = tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableSoundIdsPropertyName).Split(",");
            foreach (var soundUuid in newSoundUuids)
            {
                Assert.AreEqual(soundUuid, tableObjectSoundUuids[i]);
                i++;
            }
        }

        [TestMethod]
        public async Task UpdatePlayingSoundShouldUpdateTheSoundUuidsOfThePlayingSound()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            List<string> oldSoundUuids = new List<string>
            {
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString()
            };
            List<string> newSoundUuids = new List<string>
            {
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString()
            };
            int current = 1;
            int repetitions = 2;
            bool randomly = true;
            double volume = 0.8;

            // Create the playing sound
            await DatabaseOperations.AddPlayingSoundAsync(uuid, oldSoundUuids, current, repetitions, randomly, volume);

            // Act
            await DatabaseOperations.UpdatePlayingSoundAsync(uuid, newSoundUuids, null, null, null, null);

            // Assert
            var tableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(uuid);
            Assert.AreEqual(current, int.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableCurrentPropertyName)));
            Assert.AreEqual(repetitions, int.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableRepetitionsPropertyName)));
            Assert.AreEqual(randomly, bool.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableRandomlyPropertyName)));
            Assert.AreEqual(volume, double.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableVolumePropertyName)));

            int i = 0;
            string[] tableObjectSoundUuids = tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableSoundIdsPropertyName).Split(",");
            foreach (var soundUuid in newSoundUuids)
            {
                Assert.AreEqual(soundUuid, tableObjectSoundUuids[i]);
                i++;
            }
        }

        [TestMethod]
        public async Task UpdatePlayingSoundShouldUpdateTheCurrentOfThePlayingSound()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            List<string> soundUuids = new List<string>
            {
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString()
            };
            int oldCurrent = 1;
            int newCurrent = 0;
            int repetitions = 2;
            bool randomly = true;
            double volume = 0.8;

            // Create the playing sound
            await DatabaseOperations.AddPlayingSoundAsync(uuid, soundUuids, oldCurrent, repetitions, randomly, volume);

            // Act
            await DatabaseOperations.UpdatePlayingSoundAsync(uuid, null, newCurrent.ToString(), null, null, null);

            // Assert
            var tableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(uuid);
            Assert.AreEqual(newCurrent, int.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableCurrentPropertyName)));
            Assert.AreEqual(repetitions, int.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableRepetitionsPropertyName)));
            Assert.AreEqual(randomly, bool.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableRandomlyPropertyName)));
            Assert.AreEqual(volume, double.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableVolumePropertyName)));

            int i = 0;
            string[] tableObjectSoundUuids = tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableSoundIdsPropertyName).Split(",");
            foreach (var soundUuid in soundUuids)
            {
                Assert.AreEqual(soundUuid, tableObjectSoundUuids[i]);
                i++;
            }
        }

        [TestMethod]
        public async Task UpdatePlayingSoundShouldUpdateTheRepetitionsOfThePlayingSound()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            List<string> soundUuids = new List<string>
            {
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString()
            };
            int current = 1;
            int oldRepetitions = 2;
            int newRepetitions = 4;
            bool randomly = true;
            double volume = 0.8;

            // Create the playing sound
            await DatabaseOperations.AddPlayingSoundAsync(uuid, soundUuids, current, oldRepetitions, randomly, volume);

            // Act
            await DatabaseOperations.UpdatePlayingSoundAsync(uuid, null, null, newRepetitions.ToString(), null, null);

            // Assert
            var tableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(uuid);
            Assert.AreEqual(current, int.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableCurrentPropertyName)));
            Assert.AreEqual(newRepetitions, int.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableRepetitionsPropertyName)));
            Assert.AreEqual(randomly, bool.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableRandomlyPropertyName)));
            Assert.AreEqual(volume, double.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableVolumePropertyName)));

            int i = 0;
            string[] tableObjectSoundUuids = tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableSoundIdsPropertyName).Split(",");
            foreach (var soundUuid in soundUuids)
            {
                Assert.AreEqual(soundUuid, tableObjectSoundUuids[i]);
                i++;
            }
        }

        [TestMethod]
        public async Task UpdatePlayingSoundShouldUpdateTheRandomlyOfThePlayingSound()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            List<string> soundUuids = new List<string>
            {
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString()
            };
            int current = 1;
            int repetitions = 2;
            bool oldRandomly = false;
            bool newRandomly = true;
            double volume = 0.8;

            // Create the playing sound
            await DatabaseOperations.AddPlayingSoundAsync(uuid, soundUuids, current, repetitions, oldRandomly, volume);

            // Act
            await DatabaseOperations.UpdatePlayingSoundAsync(uuid, null, null, null, newRandomly.ToString(), null);

            // Assert
            var tableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(uuid);
            Assert.AreEqual(current, int.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableCurrentPropertyName)));
            Assert.AreEqual(repetitions, int.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableRepetitionsPropertyName)));
            Assert.AreEqual(newRandomly, bool.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableRandomlyPropertyName)));
            Assert.AreEqual(volume, double.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableVolumePropertyName)));

            int i = 0;
            string[] tableObjectSoundUuids = tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableSoundIdsPropertyName).Split(",");
            foreach (var soundUuid in soundUuids)
            {
                Assert.AreEqual(soundUuid, tableObjectSoundUuids[i]);
                i++;
            }
        }

        [TestMethod]
        public async Task UpdatePlayingSoundShouldUpdateTheVolumeOfThePlayingSound()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            List<string> soundUuids = new List<string>
            {
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString()
            };
            int current = 1;
            int repetitions = 2;
            bool randomly = true;
            double oldVolume = 0.6;
            double newVolume = 1.0;

            // Create the playing sound
            await DatabaseOperations.AddPlayingSoundAsync(uuid, soundUuids, current, repetitions, randomly, oldVolume);

            // Act
            await DatabaseOperations.UpdatePlayingSoundAsync(uuid, null, null, null, null, newVolume.ToString());

            // Assert
            var tableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(uuid);
            Assert.AreEqual(current, int.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableCurrentPropertyName)));
            Assert.AreEqual(repetitions, int.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableRepetitionsPropertyName)));
            Assert.AreEqual(randomly, bool.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableRandomlyPropertyName)));
            Assert.AreEqual(newVolume, double.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableVolumePropertyName)));

            int i = 0;
            string[] tableObjectSoundUuids = tableObjectFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableSoundIdsPropertyName).Split(",");
            foreach (var soundUuid in soundUuids)
            {
                Assert.AreEqual(soundUuid, tableObjectSoundUuids[i]);
                i++;
            }
        }
        #endregion
    }
}

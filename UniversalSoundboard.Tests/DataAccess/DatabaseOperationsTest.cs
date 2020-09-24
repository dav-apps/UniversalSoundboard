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
        #region Init
        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            ProjectInterface.RetrieveConstants = new RetrieveConstants();
            ProjectInterface.GeneralMethods = new GeneralMethods();
            ProjectInterface.LocalDataSettings = new LocalDataSettings();
            ProjectInterface.TriggerAction = new TriggerAction();

            FileManager.itemViewHolder = new UniversalSoundBoard.Common.ItemViewHolder();
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
        #endregion

        #region GetTableObjectAsync
        [TestMethod]
        public async Task GetTableObjectAsyncShouldReturnTheObject()
        {
            // Arrange
            int tableId = 23;
            var tableObject = await TableObject.CreateAsync(tableId);

            // Act
            var tableObjectFromDatabase = await DatabaseOperations.GetTableObjectAsync(tableObject.Uuid);

            // Assert
            Assert.IsNotNull(tableObjectFromDatabase);
            Assert.AreEqual(tableId, tableObjectFromDatabase.TableId);
        }

        [TestMethod]
        public async Task GetTableObjectAsyncShouldReturnNullIfTheObjectDoesNotExist()
        {
            // Arrange
            var uuid = Guid.NewGuid();

            // Act
            var tableObjectFromDatabase = await DatabaseOperations.GetTableObjectAsync(uuid);

            // Assert
            Assert.IsNull(tableObjectFromDatabase);
        }
        #endregion

        #region TableObjectExistsAsync
        [TestMethod]
        public async Task TableObjectExistsAsyncShouldReturnTrueIfTheObjectExists()
        {
            // Arrange
            int tableId = 34;
            var tableObject = await TableObject.CreateAsync(tableId);

            // Act
            bool exists = await DatabaseOperations.TableObjectExistsAsync(tableObject.Uuid);

            // Assert
            Assert.IsTrue(exists);
        }

        [TestMethod]
        public async Task TableObjectExistsAsyncShouldReturnFalseIfTheObjectDoesNotExist()
        {
            // Arrange
            var uuid = Guid.NewGuid();

            // Act
            bool exists = await DatabaseOperations.TableObjectExistsAsync(uuid);

            // Assert
            Assert.IsFalse(exists);
        }
        #endregion

        #region DeleteTableObjectAsync
        [TestMethod]
        public async Task DeleteTableObjectAsyncShouldDeleteTheObjectImmediatelyIfTheUserIsNotLoggedIn()
        {
            // Arrange
            int tableId = 21;
            var tableObject = await TableObject.CreateAsync(tableId);

            // Act
            await DatabaseOperations.DeleteTableObjectAsync(tableObject.Uuid);

            // Assert
            var tableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(tableObject.Uuid);
            Assert.IsNull(tableObjectFromDatabase);
        }

        [TestMethod]
        public async Task DeleteTableObjectAsyncShouldSetTheUploadStatusToDeletedIfTheUserIsLoggedIn()
        {
            // Arrange
            ProjectInterface.LocalDataSettings.SetValue(Dav.jwtKey, Constants.Jwt);
            int tableId = 21;
            var tableObject = await TableObject.CreateAsync(tableId);

            // Act
            await DatabaseOperations.DeleteTableObjectAsync(tableObject.Uuid);

            // Assert
            var tableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(tableObject.Uuid);
            Assert.IsNotNull(tableObjectFromDatabase);
            Assert.AreEqual(TableObject.TableObjectUploadStatus.Deleted, tableObjectFromDatabase.UploadStatus);
        }
        #endregion

        #region CreateSoundAsync
        [TestMethod]
        public async Task CreateSoundAsyncShouldCreateTheSoundObjectWithTheCorrectProperties()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            Guid soundFileUuid = Guid.NewGuid();
            List<Guid> categoryUuids = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid()
            };
            string name = "Phoenix Objection";

            // Act
            await DatabaseOperations.CreateSoundAsync(uuid, name, false, soundFileUuid, categoryUuids);

            // Assert
            var tableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(uuid);
            Assert.AreEqual(FileManager.SoundTableId, tableObjectFromDatabase.TableId);
            Assert.AreEqual(soundFileUuid, Guid.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableSoundUuidPropertyName)));
            Assert.AreEqual(name, tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableNamePropertyName));
            Assert.AreEqual(false, bool.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableFavouritePropertyName)));

            int i = 0;
            string[] tableObjectCategoryUuids = tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableCategoryUuidPropertyName).Split(",");
            foreach(var categoryUuid in categoryUuids)
            {
                Assert.AreEqual(categoryUuid, Guid.Parse(tableObjectCategoryUuids[i]));
                i++;
            }
        }

        [TestMethod]
        public async Task CreateSoundAsyncShouldCreateTheSoundObjectWithoutCategories()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            Guid soundFileUuid = Guid.NewGuid();
            string name = "Godot Objection";

            // Act
            await DatabaseOperations.CreateSoundAsync(uuid, name, false, soundFileUuid, null);

            // Assert
            var tableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(uuid);
            Assert.AreEqual(FileManager.SoundTableId, tableObjectFromDatabase.TableId);
            Assert.AreEqual(soundFileUuid, Guid.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableSoundUuidPropertyName)));
            Assert.AreEqual(name, tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableNamePropertyName));
            Assert.AreEqual(false, bool.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableFavouritePropertyName)));
            Assert.IsNull(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableCategoryUuidPropertyName));
        }
        #endregion

        #region GetAllSoundsAsync
        [TestMethod]
        public async Task GetAllSoundAsyncShouldReturnAllSounds()
        {
            // Arrange
            var firstSoundUuid = Guid.NewGuid();
            var secondSoundUuid = Guid.NewGuid();
            var firstSoundFileUuid = Guid.NewGuid();
            var secondSoundFileUuid = Guid.NewGuid();
            string firstSoundName = "Phoenix Objection";
            string secondSoundName = "Godot Hold it";

            List<Guid> firstSoundCategoryUuids = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid()
            };
            List<Guid> secondSoundCategoryUuids = new List<Guid>
            {
                Guid.NewGuid()
            };

            // Create the sounds
            await DatabaseOperations.CreateSoundAsync(firstSoundUuid, firstSoundName, false, firstSoundFileUuid, firstSoundCategoryUuids);
            await DatabaseOperations.CreateSoundAsync(secondSoundUuid, secondSoundName, false, secondSoundFileUuid, secondSoundCategoryUuids);

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
                Assert.AreEqual(categoryUuid, Guid.Parse(firstTableObjectCategoryUuids[i]));
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
                Assert.AreEqual(categoryUuid, Guid.Parse(secondTableObjectCategoryUuids[i]));
                i++;
            }
        }

        [TestMethod]
        public async Task GetAllSoundsAsyncShouldReturnEmptyListIfThereAreNoSounds()
        {
            // Act
            List<TableObject> sounds = await DatabaseOperations.GetAllSoundsAsync();

            // Assert
            Assert.AreEqual(0, sounds.Count);
        }
        #endregion

        #region UpdateSoundAsync
        [TestMethod]
        public async Task UpdateSoundAsyncShouldUpdateAllValuesOfTheSound()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            var soundFileUuid = Guid.NewGuid();
            string oldName = "Phoenix Objection";
            string newName = "Godot Objection";
            var imageFileUuid = Guid.NewGuid();
            bool newFavourite = true;
            List<Guid> oldCategoryUuids = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid()
            };
            List<Guid> newCategoryUuids = new List<Guid>
            {
                Guid.NewGuid()
            };

            // Create the sound
            await DatabaseOperations.CreateSoundAsync(uuid, oldName, false, soundFileUuid, oldCategoryUuids);

            // Act
            await DatabaseOperations.UpdateSoundAsync(uuid, newName, newFavourite, null, null, imageFileUuid, newCategoryUuids);

            // Assert
            var tableObjectFromDatabase = await DatabaseOperations.GetTableObjectAsync(uuid);
            Assert.AreEqual(soundFileUuid, Guid.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableSoundUuidPropertyName)));
            Assert.AreEqual(newName, tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableNamePropertyName));
            Assert.AreEqual(imageFileUuid, Guid.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableImageUuidPropertyName)));
            Assert.AreEqual(newFavourite, bool.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableFavouritePropertyName)));
            Assert.AreEqual(newCategoryUuids[0], tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableCategoryUuidPropertyName));
        }

        [TestMethod]
        public async Task UpdateSoundAsyncShouldUpdateTheNameOfTheSound()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            string oldName = "Phoenix Hold it";
            string newName = "Godot Objection";
            var soundFileUuid = Guid.NewGuid();

            // Create the sound
            await DatabaseOperations.CreateSoundAsync(uuid, oldName, false, soundFileUuid, null);

            // Act
            await DatabaseOperations.UpdateSoundAsync(uuid, newName, null, null, null, null, null);

            // Assert
            var tableObjectFromDatabase = await DatabaseOperations.GetTableObjectAsync(uuid);
            Assert.AreEqual(newName, tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableNamePropertyName));
            Assert.AreEqual(soundFileUuid, Guid.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableSoundUuidPropertyName)));
            Assert.IsFalse(bool.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableFavouritePropertyName)));
            Assert.IsNull(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableImageUuidPropertyName));
            Assert.IsNull(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableCategoryUuidPropertyName));
        }

        [TestMethod]
        public async Task UpdateSoundAsyncShouldUpdateTheFavouriteOfTheSound()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            string name = "Phoenix Objection";
            bool newFavourite = true;
            var soundFileUuid = Guid.NewGuid();

            // Create the sound
            await DatabaseOperations.CreateSoundAsync(uuid, name, false, soundFileUuid, null);

            // Act
            await DatabaseOperations.UpdateSoundAsync(uuid, null, newFavourite, null, null, null, null);

            // Assert
            var tableObjectFromDatabase = await DatabaseOperations.GetTableObjectAsync(uuid);
            Assert.AreEqual(name, tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableNamePropertyName));
            Assert.AreEqual(soundFileUuid, Guid.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableSoundUuidPropertyName)));
            Assert.AreEqual(newFavourite, bool.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableFavouritePropertyName)));
            Assert.IsNull(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableImageUuidPropertyName));
            Assert.IsNull(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableCategoryUuidPropertyName));
        }

        [TestMethod]
        public async Task UpdateSoundAsyncShouldUpdateTheImageUuidOfTheSound()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            string name = "Phoenix Objection";
            var soundFileUuid = Guid.NewGuid();
            var newImageUuid = Guid.NewGuid();

            // Create the sound
            await DatabaseOperations.CreateSoundAsync(uuid, name, false, soundFileUuid, null);

            // Act
            await DatabaseOperations.UpdateSoundAsync(uuid, null, null, null, null, newImageUuid, null);

            // Assert
            var tableObjectFromDatabase = await DatabaseOperations.GetTableObjectAsync(uuid);
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
            List<Guid> oldCategories = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid()
            };
            List<Guid> newCategories = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid()
            };

            // Create the sound
            await DatabaseOperations.CreateSoundAsync(uuid, name, false, soundFileUuid, oldCategories);

            // Act
            await DatabaseOperations.UpdateSoundAsync(uuid, null, null, null, null, null, newCategories);

            // Assert
            var tableObjectFromDatabase = await DatabaseOperations.GetTableObjectAsync(uuid);
            Assert.AreEqual(name, tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableNamePropertyName));
            Assert.AreEqual(soundFileUuid, Guid.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableSoundUuidPropertyName)));
            Assert.IsFalse(bool.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableFavouritePropertyName)));
            Assert.IsNull(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableImageUuidPropertyName));

            int i = 0;
            string[] categoryUuids = tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableCategoryUuidPropertyName).Split(",");
            foreach (var categoryUuid in newCategories)
            {
                Assert.AreEqual(categoryUuid, Guid.Parse(categoryUuids[i]));
                i++;
            }
        }
        #endregion

        #region DeleteSoundAsync
        [TestMethod]
        public async Task DeleteSoundAsyncShouldDeleteTheSound()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            string name = "Phoenix Objection";

            // Create the sound
            await DatabaseOperations.CreateSoundAsync(uuid, name, false, Guid.NewGuid(), null);

            // Act
            await DatabaseOperations.DeleteSoundAsync(uuid);

            // Assert
            var tableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(uuid);
            Assert.IsNull(tableObjectFromDatabase);
        }

        [TestMethod]
        public async Task DeleteSoundAsyncShouldDeleteTheSoundAndTheSoundFile()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            var soundFileUuid = Guid.NewGuid();
            string name = "Phoenix Objection";
            List<Guid> categoryUuids = new List<Guid>
            {
                Guid.NewGuid()
            };

            // Create the sound
            await DatabaseOperations.CreateSoundAsync(uuid, name, false, soundFileUuid, categoryUuids);

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
        public async Task DeleteSoundAsyncShouldDeleteTheSoundTheSoundFileAndTheImageFile()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            var soundFileUuid = Guid.NewGuid();
            var imageFileUuid = Guid.NewGuid();
            string name = "Phoenix Objection";

            // Create the sound
            await DatabaseOperations.CreateSoundAsync(uuid, name, false, soundFileUuid, null);
            await DatabaseOperations.UpdateSoundAsync(uuid, null, null, null, null, imageFileUuid, null);

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

        #region CreateCategoryAsync
        [TestMethod]
        public async Task CreateCategoryAsyncShouldCreateTheCategoryObjectWithTheCorrectProperties()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            string name = "Testcategory";
            string icon = "icon";

            // Act
            await DatabaseOperations.CreateCategoryAsync(uuid, null, name, icon);

            // Assert
            var tableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(uuid);
            Assert.AreEqual(FileManager.CategoryTableId, tableObjectFromDatabase.TableId);
            Assert.AreEqual(name, tableObjectFromDatabase.GetPropertyValue(FileManager.CategoryTableNamePropertyName));
            Assert.AreEqual(icon, tableObjectFromDatabase.GetPropertyValue(FileManager.CategoryTableIconPropertyName));
        }
        #endregion

        #region GetAllCategoriesAsync
        [TestMethod]
        public async Task GetAllCategoriesAsyncShouldReturnAllCategories()
        {
            // Arrange
            var firstUuid = Guid.NewGuid();
            var secondUuid = Guid.NewGuid();
            string firstName = "FirstName";
            string secondName = "SecondName";
            string firstIcon = "FirstIcon";
            string secondIcon = "SecondIcon";

            // Create the categories
            await DatabaseOperations.CreateCategoryAsync(firstUuid, null, firstName, firstIcon);
            await DatabaseOperations.CreateCategoryAsync(secondUuid, null, secondName, secondIcon);

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
        public async Task GetAllCategoriesAsyncShouldReturnEmptyListIfThereAreNoCategories()
        {
            // Act
            List<TableObject> categories = await DatabaseOperations.GetAllCategoriesAsync();

            // Assert
            Assert.AreEqual(0, categories.Count);
        }
        #endregion

        #region UpdateCategoryAsync
        [TestMethod]
        public async Task UpdateCategoryAsyncShouldUpdateAllValuesOfTheCategory()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            var oldName = "TestCategory";
            var newName = "UpdatedCategory";
            var oldIcon = "icon";
            var newIcon = "updatedIcon";

            // Create the category
            await DatabaseOperations.CreateCategoryAsync(uuid, null, oldName, oldIcon);

            // Act
            await DatabaseOperations.UpdateCategoryAsync(uuid, newName, newIcon, null);

            // Assert
            var tableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(uuid);
            Assert.AreEqual(newName, tableObjectFromDatabase.GetPropertyValue(FileManager.CategoryTableNamePropertyName));
            Assert.AreEqual(newIcon, tableObjectFromDatabase.GetPropertyValue(FileManager.CategoryTableIconPropertyName));
        }

        [TestMethod]
        public async Task UpdateCategoryAsyncShouldUpdateTheNameOfTheCategory()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            var oldName = "Test category";
            var newName = "Updated category";
            var icon = "icon";

            // Create the category
            await DatabaseOperations.CreateCategoryAsync(uuid, null, oldName, icon);

            // Act
            await DatabaseOperations.UpdateCategoryAsync(uuid, newName, null, null);

            // Assert
            var tableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(uuid);
            Assert.AreEqual(newName, tableObjectFromDatabase.GetPropertyValue(FileManager.CategoryTableNamePropertyName));
            Assert.AreEqual(icon, tableObjectFromDatabase.GetPropertyValue(FileManager.CategoryTableIconPropertyName));
        }

        [TestMethod]
        public async Task UpdateCategoryAsyncShouldUpdateTheIconOfTheCategory()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            var name = "TestCategory";
            var oldIcon = "icon";
            var newIcon = "updated icon";

            // Create the category
            await DatabaseOperations.CreateCategoryAsync(uuid, null, name, oldIcon);

            // Act
            await DatabaseOperations.UpdateCategoryAsync(uuid, null, newIcon, null);

            // Assert
            var tableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(uuid);
            Assert.AreEqual(name, tableObjectFromDatabase.GetPropertyValue(FileManager.CategoryTableNamePropertyName));
            Assert.AreEqual(newIcon, tableObjectFromDatabase.GetPropertyValue(FileManager.CategoryTableIconPropertyName));
        }
        #endregion

        #region CreatePlayingSoundAsync
        [TestMethod]
        public async Task CreatePlayingSoundAsyncShouldCreateThePlayingSoundObjectWithTheCorrectProperties()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            List<Guid> soundUuids = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid()
            };
            int current = 1;
            int repetitions = 3;
            bool randomly = true;
            int volume = 40;

            // Act
            await DatabaseOperations.CreatePlayingSoundAsync(uuid, soundUuids, current, repetitions, randomly, volume, false);

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

        #region GetAllPlayingSoundsAsync
        [TestMethod]
        public async Task GetAllPlayingSoundsAsyncShouldReturnAllPlayingSounds()
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
            int firstVolume = 60;
            int secondVolume = 20;

            List<Guid> firstSoundUuids = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid()
            };
            List<Guid> secondSoundUuids = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid()
            };

            // Create the playing sounds
            await DatabaseOperations.CreatePlayingSoundAsync(firstUuid, firstSoundUuids, firstCurrent, firstRepetitions, firstRandomly, firstVolume, false);
            await DatabaseOperations.CreatePlayingSoundAsync(secondUuid, secondSoundUuids, secondCurrent, secondRepetitions, secondRandomly, secondVolume, false);

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
        public async Task GetAllPlayingSoundsAsyncShouldReturnEmptyListIfThereAreNoPlayingSounds()
        {
            // Act
            List<TableObject> playingSounds = await DatabaseOperations.GetAllPlayingSoundsAsync();

            // Assert
            Assert.AreEqual(0, playingSounds.Count);
        }
        #endregion

        #region UpdatePlayingSoundAsync
        [TestMethod]
        public async Task UpdatePlayingSoundAsyncShouldUpdateAllValuesOfThePlayingSound()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            List<Guid> oldSoundUuids = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid()
            };
            List<Guid> newSoundUuids = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid()
            };
            int oldCurrent = 0;
            int newCurrent = 1;
            int oldRepetitions = 3;
            int newRepetitions = 5;
            bool oldRandomly = true;
            bool newRandomly = false;
            int oldVolume = 40;
            int newVolume = 96;

            // Create the playing sound
            await DatabaseOperations.CreatePlayingSoundAsync(uuid, oldSoundUuids, oldCurrent, oldRepetitions, oldRandomly, oldVolume, false);

            // Act
            await DatabaseOperations.UpdatePlayingSoundAsync(uuid, newSoundUuids, newCurrent, newRepetitions, newRandomly, newVolume, null);

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
        public async Task UpdatePlayingSoundAsyncShouldUpdateTheSoundUuidsOfThePlayingSound()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            List<Guid> oldSoundUuids = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid()
            };
            List<Guid> newSoundUuids = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid()
            };
            int current = 1;
            int repetitions = 2;
            bool randomly = true;
            int volume = 80;

            // Create the playing sound
            await DatabaseOperations.CreatePlayingSoundAsync(uuid, oldSoundUuids, current, repetitions, randomly, volume, false);

            // Act
            await DatabaseOperations.UpdatePlayingSoundAsync(uuid, newSoundUuids, null, null, null, null, null);

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
        public async Task UpdatePlayingSoundAsyncShouldUpdateTheCurrentOfThePlayingSound()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            List<Guid> soundUuids = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid()
            };
            int oldCurrent = 1;
            int newCurrent = 0;
            int repetitions = 2;
            bool randomly = true;
            int volume = 80;

            // Create the playing sound
            await DatabaseOperations.CreatePlayingSoundAsync(uuid, soundUuids, oldCurrent, repetitions, randomly, volume, false);

            // Act
            await DatabaseOperations.UpdatePlayingSoundAsync(uuid, null, newCurrent, null, null, null, null);

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
        public async Task UpdatePlayingSoundAsyncShouldUpdateTheRepetitionsOfThePlayingSound()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            List<Guid> soundUuids = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid()
            };
            int current = 1;
            int oldRepetitions = 2;
            int newRepetitions = 4;
            bool randomly = true;
            int volume = 80;

            // Create the playing sound
            await DatabaseOperations.CreatePlayingSoundAsync(uuid, soundUuids, current, oldRepetitions, randomly, volume, false);

            // Act
            await DatabaseOperations.UpdatePlayingSoundAsync(uuid, null, null, newRepetitions, null, null, null);

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
        public async Task UpdatePlayingSoundAsyncShouldUpdateTheRandomlyOfThePlayingSound()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            List<Guid> soundUuids = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid()
            };
            int current = 1;
            int repetitions = 2;
            bool oldRandomly = false;
            bool newRandomly = true;
            int volume = 80;

            // Create the playing sound
            await DatabaseOperations.CreatePlayingSoundAsync(uuid, soundUuids, current, repetitions, oldRandomly, volume, false);

            // Act
            await DatabaseOperations.UpdatePlayingSoundAsync(uuid, null, null, null, newRandomly, null, null);

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
        public async Task UpdatePlayingSoundAsyncShouldUpdateTheVolumeOfThePlayingSound()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            List<Guid> soundUuids = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid()
            };
            int current = 1;
            int repetitions = 2;
            bool randomly = true;
            int oldVolume = 60;
            int newVolume = 100;

            // Create the playing sound
            await DatabaseOperations.CreatePlayingSoundAsync(uuid, soundUuids, current, repetitions, randomly, oldVolume, false);

            // Act
            await DatabaseOperations.UpdatePlayingSoundAsync(uuid, null, null, null, null, newVolume, null);

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

        #region GetAllOrdersAsync
        [TestMethod]
        public async Task GetAllOrdersAsyncShouldReturnAllOrders()
        {
            // Arrange
            var firstUuid = Guid.NewGuid();
            var secondUuid = Guid.NewGuid();

            // Create the orders
            await TableObject.CreateAsync(firstUuid, FileManager.OrderTableId);
            await TableObject.CreateAsync(secondUuid, FileManager.OrderTableId);

            // Act
            List<TableObject> orders = await DatabaseOperations.GetAllOrdersAsync();

            // Assert
            Assert.AreEqual(2, orders.Count);
        }

        [TestMethod]
        public async Task GetAllOrdersAsyncShouldReturnEmptyListIfThereAreNoOrders()
        {
            // Act
            List<TableObject> orders = await DatabaseOperations.GetAllOrdersAsync();

            // Assert
            Assert.AreEqual(0, orders.Count);
        }
        #endregion

        #region SetCategoryOrderAsync
        [TestMethod]
        public async Task SetCategoryOrderAsyncShouldCreateNewOrderWithTheCorrectProperties()
        {
            // Arrange
            List<Guid> uuids = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
            };

            // Act
            await DatabaseOperations.SetCategoryOrderAsync(Guid.NewGuid(), uuids);

            // Assert
            var tableObjects = await DatabaseOperations.GetAllOrdersAsync();
            Assert.AreEqual(1, tableObjects.Count);
            var categoryOrderTableObject = tableObjects.Find(obj => obj.GetPropertyValue(FileManager.OrderTableTypePropertyName) == FileManager.CategoryOrderType);
            Assert.IsNotNull(categoryOrderTableObject);
            Assert.AreEqual(uuids.Count + 1, categoryOrderTableObject.Properties.Count);
            
            for(int i = 0; i < uuids.Count; i++)
            {
                string value = categoryOrderTableObject.GetPropertyValue(i.ToString());
                Assert.AreEqual(uuids[i], Guid.Parse(value));
            }
        }

        [TestMethod]
        public async Task SetCategoryOrderAsyncShouldUpdateTheExistingOrderWithTheCorrectProperties()
        {
            // Arrange
            List<Guid> oldUuids = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
            };
            List<Guid> newUuids = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
            };

            // Create the category order
            await DatabaseOperations.SetCategoryOrderAsync(Guid.NewGuid(), oldUuids);

            // Act
            await DatabaseOperations.SetCategoryOrderAsync(Guid.NewGuid(), newUuids);

            // Assert
            var tableObjects = await DatabaseOperations.GetAllOrdersAsync();
            Assert.AreEqual(1, tableObjects.Count);
            var categoryOrderTableObject = tableObjects.Find(obj => obj.GetPropertyValue(FileManager.OrderTableTypePropertyName) == FileManager.CategoryOrderType);
            Assert.IsNotNull(categoryOrderTableObject);
            Assert.AreEqual(newUuids.Count + 1, categoryOrderTableObject.Properties.Count);

            for (int i = 0; i < newUuids.Count; i++)
            {
                string value = categoryOrderTableObject.GetPropertyValue(i.ToString());
                Assert.AreEqual(newUuids[i], Guid.Parse(value));
            }
        }

        [TestMethod]
        public async Task SetCategoryOrderAsyncShouldUpdateTheExistingOrderAndRemoveTheRedundantUuids()
        {
            // Arrange
            List<Guid> oldUuids = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
            };
            List<Guid> newUuids = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
            };

            // Create the category order
            await DatabaseOperations.SetCategoryOrderAsync(Guid.NewGuid(), oldUuids);

            // Act
            await DatabaseOperations.SetCategoryOrderAsync(Guid.NewGuid(), newUuids);

            // Assert
            var tableObjects = await DatabaseOperations.GetAllOrdersAsync();
            Assert.AreEqual(1, tableObjects.Count);
            var categoryOrderTableObject = tableObjects.Find(obj => obj.GetPropertyValue(FileManager.OrderTableTypePropertyName) == FileManager.CategoryOrderType);
            Assert.IsNotNull(categoryOrderTableObject);
            Assert.AreEqual(newUuids.Count + 1, categoryOrderTableObject.Properties.Count);

            for (int i = 0; i < newUuids.Count; i++)
            {
                string value = categoryOrderTableObject.GetPropertyValue(i.ToString());
                Assert.AreEqual(newUuids[i], Guid.Parse(value));
            }
        }
        #endregion

        #region SetSoundOrderAsync
        [TestMethod]
        public async Task SetSoundOrderAsyncShouldCreateNewOrderForTheCategory()
        {
            // Arrange
            var firstCategoryUuid = Guid.NewGuid();
            var secondCategoryUuid = Guid.NewGuid();

            List<Guid> firstCategoryUuids = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid()
            };
            List<Guid> secondCategoryUuids = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
            };

            // Create the first sound order
            await DatabaseOperations.SetSoundOrderAsync(firstCategoryUuid, false, firstCategoryUuids);
            var orders = await DatabaseOperations.GetAllOrdersAsync();
            Assert.AreEqual(1, orders.Count);
            
            // Create the second sound order
            await DatabaseOperations.SetSoundOrderAsync(secondCategoryUuid, false, secondCategoryUuids);
            orders = await DatabaseOperations.GetAllOrdersAsync();
            Assert.AreEqual(2, orders.Count);

            // Test the first sound order
            Assert.AreEqual(FileManager.SoundOrderType, orders[0].GetPropertyValue(FileManager.OrderTableTypePropertyName));
            Assert.AreEqual(firstCategoryUuid, Guid.Parse(orders[0].GetPropertyValue(FileManager.OrderTableCategoryPropertyName)));
            Assert.IsFalse(bool.Parse(orders[0].GetPropertyValue(FileManager.OrderTableFavouritePropertyName)));

            for (int i = 0; i < firstCategoryUuids.Count; i++)
            {
                string value = orders[0].GetPropertyValue(i.ToString());
                Assert.AreEqual(firstCategoryUuids[i], Guid.Parse(value));
            }

            // Test the second sound order
            Assert.AreEqual(FileManager.SoundOrderType, orders[1].GetPropertyValue(FileManager.OrderTableTypePropertyName));
            Assert.AreEqual(secondCategoryUuid, Guid.Parse(orders[1].GetPropertyValue(FileManager.OrderTableCategoryPropertyName)));
            Assert.IsFalse(bool.Parse(orders[1].GetPropertyValue(FileManager.OrderTableFavouritePropertyName)));

            for (int i = 0; i < secondCategoryUuids.Count; i++)
            {
                string value = orders[1].GetPropertyValue(i.ToString());
                Assert.AreEqual(secondCategoryUuids[i], Guid.Parse(value));
            }
        }

        [TestMethod]
        public async Task SetSoundOrderAsyncShouldCreateNewOrderForTheFavouritesOfTheCategory()
        {
            // Arrange
            var categoryUuid = Guid.NewGuid();

            List<Guid> normalUuids = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid()
            };
            List<Guid> favouriteUuids = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
            };

            // Create the normal sound order
            await DatabaseOperations.SetSoundOrderAsync(categoryUuid, false, normalUuids);
            var orders = await DatabaseOperations.GetAllOrdersAsync();
            Assert.AreEqual(1, orders.Count);

            // Create the favourites sound order
            await DatabaseOperations.SetSoundOrderAsync(categoryUuid, true, favouriteUuids);
            orders = await DatabaseOperations.GetAllOrdersAsync();
            Assert.AreEqual(2, orders.Count);

            // Test the normal sound order
            Assert.AreEqual(FileManager.SoundOrderType, orders[0].GetPropertyValue(FileManager.OrderTableTypePropertyName));
            Assert.AreEqual(categoryUuid, Guid.Parse(orders[0].GetPropertyValue(FileManager.OrderTableCategoryPropertyName)));
            Assert.IsFalse(bool.Parse(orders[0].GetPropertyValue(FileManager.OrderTableFavouritePropertyName)));

            for (int i = 0; i < normalUuids.Count; i++)
            {
                string value = orders[0].GetPropertyValue(i.ToString());
                Assert.AreEqual(normalUuids[i], Guid.Parse(value));
            }

            // Test the favourites sound order
            Assert.AreEqual(FileManager.SoundOrderType, orders[1].GetPropertyValue(FileManager.OrderTableTypePropertyName));
            Assert.AreEqual(categoryUuid, Guid.Parse(orders[1].GetPropertyValue(FileManager.OrderTableCategoryPropertyName)));
            Assert.IsTrue(bool.Parse(orders[1].GetPropertyValue(FileManager.OrderTableFavouritePropertyName)));

            for (int i = 0; i < favouriteUuids.Count; i++)
            {
                string value = orders[1].GetPropertyValue(i.ToString());
                Assert.AreEqual(favouriteUuids[i], Guid.Parse(value));
            }
        }

        [TestMethod]
        public async Task SetSoundOrderAsyncShouldUpdateTheExistingOrderWithTheCorrectProperties()
        {
            // Arrange
            var categoryUuid = Guid.NewGuid();
            List<Guid> oldUuids = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
            };
            List<Guid> newUuids = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
            };

            // Create the sound order
            await DatabaseOperations.SetSoundOrderAsync(categoryUuid, false, oldUuids);
            var orders = await DatabaseOperations.GetAllOrdersAsync();
            Assert.AreEqual(1, orders.Count);

            // Act
            await DatabaseOperations.SetSoundOrderAsync(categoryUuid, false, newUuids);

            // Assert
            orders = await DatabaseOperations.GetAllOrdersAsync();
            Assert.AreEqual(1, orders.Count);

            Assert.AreEqual(FileManager.SoundOrderType, orders[0].GetPropertyValue(FileManager.OrderTableTypePropertyName));
            Assert.AreEqual(categoryUuid, Guid.Parse(orders[0].GetPropertyValue(FileManager.OrderTableCategoryPropertyName)));
            Assert.IsFalse(bool.Parse(orders[0].GetPropertyValue(FileManager.OrderTableFavouritePropertyName)));

            for (int i = 0; i < newUuids.Count; i++)
            {
                string value = orders[0].GetPropertyValue(i.ToString());
                Assert.AreEqual(newUuids[i], Guid.Parse(value));
            }
        }

        [TestMethod]
        public async Task SetSoundOrderAsyncShouldUpdateTheExistingOrderAndRemoveTheRedundantUuids()
        {
            // Arrange
            var categoryUuid = Guid.NewGuid();
            List<Guid> oldUuids = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
            };
            List<Guid> newUuids = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid()
            };

            // Create the sound order
            await DatabaseOperations.SetSoundOrderAsync(categoryUuid, false, oldUuids);
            var orders = await DatabaseOperations.GetAllOrdersAsync();
            Assert.AreEqual(1, orders.Count);

            // Act
            await DatabaseOperations.SetSoundOrderAsync(categoryUuid, false, newUuids);

            // Assert
            orders = await DatabaseOperations.GetAllOrdersAsync();
            Assert.AreEqual(1, orders.Count);

            var soundOrderTableObject = orders[0];
            Assert.AreEqual(newUuids.Count + 3, soundOrderTableObject.Properties.Count);
            Assert.AreEqual(FileManager.SoundOrderType, soundOrderTableObject.GetPropertyValue(FileManager.OrderTableTypePropertyName));
            Assert.AreEqual(categoryUuid, Guid.Parse(soundOrderTableObject.GetPropertyValue(FileManager.OrderTableCategoryPropertyName)));
            Assert.IsFalse(bool.Parse(soundOrderTableObject.GetPropertyValue(FileManager.OrderTableFavouritePropertyName)));

            for (int i = 0; i < newUuids.Count; i++)
            {
                string value = soundOrderTableObject.GetPropertyValue(i.ToString());
                Assert.AreEqual(newUuids[i], Guid.Parse(value));
            }
        }
        #endregion
    }
}

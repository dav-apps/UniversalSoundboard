using davClassLibrary;
using davClassLibrary.Common;
using davClassLibrary.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Tests.Common;

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

            FileManager.itemViewHolder = new UniversalSoundboard.Common.ItemViewHolder();
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

        #region General Methods
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

        #region GetTableObjectsByPropertyAsync
        [TestMethod]
        public async Task GetTableObjectsByPropertyAsyncShouldReturnTheCorrectTableObjects()
        {
            // Arrange
            Guid firstTableObjectUuid = Guid.NewGuid();
            Guid secondTableObjectUuid = Guid.NewGuid();
            Guid thirdTableObjectUuid = Guid.NewGuid();

            string searchedPropertyName = "test";
            string searchedPropertyValue = "12345";

            var firstTableObject = await TableObject.CreateAsync(firstTableObjectUuid, 12);
            var secondTableObject = await TableObject.CreateAsync(secondTableObjectUuid, 12);
            var thirdTableObject = await TableObject.CreateAsync(thirdTableObjectUuid, 12);

            await Property.CreateAsync(firstTableObject.Id, searchedPropertyName, searchedPropertyValue);
            await Property.CreateAsync(firstTableObject.Id, "bla", "testtest");

            await Property.CreateAsync(secondTableObject.Id, "231", "asdasfaspgs");
            await Property.CreateAsync(secondTableObject.Id, searchedPropertyName, "sadasdasd");

            await Property.CreateAsync(thirdTableObject.Id, "98435", "asdasdasd");
            await Property.CreateAsync(thirdTableObject.Id, searchedPropertyName, searchedPropertyValue);

            // Act
            var tableObjectsFromDatabase = await DatabaseOperations.GetTableObjectsByPropertyAsync(searchedPropertyName, searchedPropertyValue);

            // Assert
            Assert.AreEqual(2, tableObjectsFromDatabase.Count);

            Assert.AreEqual(firstTableObjectUuid, tableObjectsFromDatabase[0].Uuid);
            Assert.AreEqual(searchedPropertyValue, tableObjectsFromDatabase[0].GetPropertyValue(searchedPropertyName));

            Assert.AreEqual(thirdTableObjectUuid, tableObjectsFromDatabase[1].Uuid);
            Assert.AreEqual(searchedPropertyValue, tableObjectsFromDatabase[1].GetPropertyValue(searchedPropertyName));
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
            Guid uuid = Guid.NewGuid();
            int tableId = 21;
            await TableObject.CreateAsync(uuid, tableId);

            // Assert (1)
            var tableObjectFromDatabase = await DatabaseOperations.GetTableObjectAsync(uuid);
            Assert.IsNotNull(tableObjectFromDatabase);
            Assert.AreEqual(uuid, tableObjectFromDatabase.Uuid);
            Assert.AreEqual(tableId, tableObjectFromDatabase.TableId);

            // Act
            await DatabaseOperations.DeleteTableObjectAsync(uuid);

            // Assert (2)
            tableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(uuid);
            Assert.IsNull(tableObjectFromDatabase);
        }

        [TestMethod]
        public async Task DeleteTableObjectAsyncShouldSetTheUploadStatusToDeletedIfTheUserIsLoggedIn()
        {
            // Arrange
            ProjectInterface.LocalDataSettings.SetValue(Dav.jwtKey, Constants.Jwt);
            Guid uuid = Guid.NewGuid();
            int tableId = 21;
            await TableObject.CreateAsync(uuid, tableId);

            // Assert (1)
            var tableObjectFromDatabase = await DatabaseOperations.GetTableObjectAsync(uuid);
            Assert.IsNotNull(tableObjectFromDatabase);
            Assert.AreEqual(uuid, tableObjectFromDatabase.Uuid);
            Assert.AreEqual(tableId, tableObjectFromDatabase.TableId);

            // Act
            await DatabaseOperations.DeleteTableObjectAsync(uuid);

            // Assert (2)
            tableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(uuid);
            Assert.IsNotNull(tableObjectFromDatabase);
            Assert.AreEqual(uuid, tableObjectFromDatabase.Uuid);
            Assert.AreEqual(tableId, tableObjectFromDatabase.TableId);
            Assert.AreEqual(TableObject.TableObjectUploadStatus.Deleted, tableObjectFromDatabase.UploadStatus);
        }
        #endregion
        #endregion

        #region Sound
        #region CreateSoundAsync
        [TestMethod]
        public async Task CreateSoundAsyncShouldCreateTheSoundObjectWithTheCorrectProperties()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            Guid soundFileUuid = Guid.NewGuid();
            string name = "Phoenix Objection";
            bool favourite = true;
            List<Guid> categoryUuids = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid()
            };

            // Act
            await DatabaseOperations.CreateSoundAsync(uuid, name, favourite, soundFileUuid, categoryUuids);

            // Assert
            var tableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(uuid);
            Assert.IsNotNull(tableObjectFromDatabase);
            Assert.AreEqual(uuid, tableObjectFromDatabase.Uuid);
            Assert.AreEqual(FileManager.SoundTableId, tableObjectFromDatabase.TableId);
            Assert.AreEqual(soundFileUuid.ToString(), tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableSoundUuidPropertyName));
            Assert.AreEqual(name, tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableNamePropertyName));
            Assert.AreEqual(favourite, bool.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableFavouritePropertyName)));

            int i = 0;
            string[] tableObjectCategoryUuids = tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableCategoryUuidPropertyName).Split(",");
            foreach (var categoryUuid in categoryUuids)
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
            bool favourite = false;

            // Act
            await DatabaseOperations.CreateSoundAsync(uuid, name, favourite, soundFileUuid, null);

            // Assert
            var tableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(uuid);
            Assert.AreEqual(FileManager.SoundTableId, tableObjectFromDatabase.TableId);
            Assert.AreEqual(soundFileUuid, Guid.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableSoundUuidPropertyName)));
            Assert.AreEqual(name, tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableNamePropertyName));
            Assert.AreEqual(favourite, bool.Parse(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableFavouritePropertyName)));
            Assert.IsNull(tableObjectFromDatabase.GetPropertyValue(FileManager.SoundTableCategoryUuidPropertyName));
        }
        #endregion

        #region GetAllSoundsAsync
        [TestMethod]
        public async Task GetAllSoundsAsyncShouldReturnAllSounds()
        {
            // Arrange
            Guid firstSoundUuid = Guid.NewGuid();
            Guid secondSoundUuid = Guid.NewGuid();
            Guid thirdSoundUuid = Guid.NewGuid();

            string firstSoundName = "TestSound 1";
            string secondSoundName = "Second test sound";
            string thirdSoundName = "3rd sound";

            bool firstSoundFavourite = false;
            bool secondSoundFavourite = true;
            bool thirdSoundFavourite = true;

            Guid firstSoundFileUuid = Guid.NewGuid();
            Guid secondSoundFileUuid = Guid.NewGuid();
            Guid thirdSoundFileUuid = Guid.NewGuid();

            List<Guid> firstSoundCategories = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            List<Guid> secondSoundCategories = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            List<Guid> thirdSoundCategories = new List<Guid> { Guid.NewGuid() };

            await DatabaseOperations.CreateSoundAsync(firstSoundUuid, firstSoundName, firstSoundFavourite, firstSoundFileUuid, firstSoundCategories);
            await DatabaseOperations.CreateSoundAsync(secondSoundUuid, secondSoundName, secondSoundFavourite, secondSoundFileUuid, secondSoundCategories);
            await DatabaseOperations.CreateSoundAsync(thirdSoundUuid, thirdSoundName, thirdSoundFavourite, thirdSoundFileUuid, thirdSoundCategories);

            // Act
            List<TableObject> sounds = await DatabaseOperations.GetAllSoundsAsync();

            // Assert
            Assert.AreEqual(3, sounds.Count);

            Assert.AreEqual(firstSoundUuid, sounds[0].Uuid);
            Assert.AreEqual(firstSoundName, sounds[0].GetPropertyValue(FileManager.SoundTableNamePropertyName));
            Assert.AreEqual(firstSoundFavourite, bool.Parse(sounds[0].GetPropertyValue(FileManager.SoundTableFavouritePropertyName)));
            Assert.AreEqual(firstSoundFileUuid.ToString(), sounds[0].GetPropertyValue(FileManager.SoundTableSoundUuidPropertyName));

            string[] firstSoundTableObjectCategories = sounds[0].GetPropertyValue(FileManager.SoundTableCategoryUuidPropertyName).Split(',');
            Assert.AreEqual(firstSoundCategories.Count, firstSoundTableObjectCategories.Length);
            for (int i = 0; i < firstSoundCategories.Count; i++)
                Assert.AreEqual(firstSoundCategories[i].ToString(), firstSoundTableObjectCategories[i]);

            Assert.AreEqual(secondSoundUuid, sounds[1].Uuid);
            Assert.AreEqual(secondSoundName, sounds[1].GetPropertyValue(FileManager.SoundTableNamePropertyName));
            Assert.AreEqual(secondSoundFavourite, bool.Parse(sounds[1].GetPropertyValue(FileManager.SoundTableFavouritePropertyName)));
            Assert.AreEqual(secondSoundFileUuid.ToString(), sounds[1].GetPropertyValue(FileManager.SoundTableSoundUuidPropertyName));

            string[] secondSoundTableObjectCategories = sounds[1].GetPropertyValue(FileManager.SoundTableCategoryUuidPropertyName).Split(',');
            Assert.AreEqual(secondSoundCategories.Count, secondSoundTableObjectCategories.Length);
            for (int i = 0; i < secondSoundCategories.Count; i++)
                Assert.AreEqual(secondSoundCategories[i].ToString(), secondSoundTableObjectCategories[i]);

            Assert.AreEqual(thirdSoundUuid, sounds[2].Uuid);
            Assert.AreEqual(thirdSoundName, sounds[2].GetPropertyValue(FileManager.SoundTableNamePropertyName));
            Assert.AreEqual(thirdSoundFavourite, bool.Parse(sounds[2].GetPropertyValue(FileManager.SoundTableFavouritePropertyName)));
            Assert.AreEqual(thirdSoundFileUuid.ToString(), sounds[2].GetPropertyValue(FileManager.SoundTableSoundUuidPropertyName));

            string[] thirdSoundTableObjectCategories = sounds[2].GetPropertyValue(FileManager.SoundTableCategoryUuidPropertyName).Split(',');
            Assert.AreEqual(thirdSoundCategories.Count, thirdSoundTableObjectCategories.Length);
            for (int i = 0; i < thirdSoundCategories.Count; i++)
                Assert.AreEqual(thirdSoundCategories[i].ToString(), thirdSoundTableObjectCategories[i]);
        }
        #endregion

        #region UpdateSoundAsync
        [TestMethod]
        public async Task UpdateSoundAsyncShouldUpdateAllValuesOfTheSound()
        {
            // Arrange (1)
            Guid uuid = Guid.NewGuid();
            string name = "Test-Sound";
            bool favourite = false;
            Guid fileUuid = Guid.NewGuid();
            List<Guid> categoryUuids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

            // Act (1)
            await DatabaseOperations.CreateSoundAsync(uuid, name, favourite, fileUuid, categoryUuids);

            // Assert (1)
            TableObject soundFromDatabase = await DatabaseOperations.GetTableObjectAsync(uuid);
            Assert.IsNotNull(soundFromDatabase);
            Assert.AreEqual(uuid, soundFromDatabase.Uuid);
            Assert.AreEqual(name, soundFromDatabase.GetPropertyValue(FileManager.SoundTableNamePropertyName));
            Assert.AreEqual(favourite, bool.Parse(soundFromDatabase.GetPropertyValue(FileManager.SoundTableFavouritePropertyName)));
            Assert.AreEqual(fileUuid.ToString(), soundFromDatabase.GetPropertyValue(FileManager.SoundTableSoundUuidPropertyName));

            string[] soundTableObjectCategories = soundFromDatabase.GetPropertyValue(FileManager.SoundTableCategoryUuidPropertyName).Split(',');
            Assert.AreEqual(categoryUuids.Count, soundTableObjectCategories.Length);
            for (int i = 0; i < categoryUuids.Count; i++)
                Assert.AreEqual(categoryUuids[i].ToString(), soundTableObjectCategories[i]);

            // Arrange (2)
            string updatedName = "Updated Test-Sound name";
            bool updatedFavourite = true;
            int defaultVolume = 80;
            bool defaultMuted = true;
            Guid imageUuid = Guid.NewGuid();
            List<Guid> updatedCategoryUuids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

            // Act (2)
            await DatabaseOperations.UpdateSoundAsync(uuid, updatedName, updatedFavourite, defaultVolume, defaultMuted, imageUuid, updatedCategoryUuids);

            // Assert (2)
            soundFromDatabase = await DatabaseOperations.GetTableObjectAsync(uuid);
            Assert.IsNotNull(soundFromDatabase);
            Assert.AreEqual(updatedName, soundFromDatabase.GetPropertyValue(FileManager.SoundTableNamePropertyName));
            Assert.AreEqual(updatedFavourite, bool.Parse(soundFromDatabase.GetPropertyValue(FileManager.SoundTableFavouritePropertyName)));
            Assert.AreEqual(fileUuid.ToString(), soundFromDatabase.GetPropertyValue(FileManager.SoundTableSoundUuidPropertyName));
            Assert.AreEqual(defaultVolume, int.Parse(soundFromDatabase.GetPropertyValue(FileManager.SoundTableDefaultVolumePropertyName)));
            Assert.AreEqual(defaultMuted, bool.Parse(soundFromDatabase.GetPropertyValue(FileManager.SoundTableDefaultMutedPropertyName)));
            Assert.AreEqual(imageUuid.ToString(), soundFromDatabase.GetPropertyValue(FileManager.SoundTableImageUuidPropertyName));

            soundTableObjectCategories = soundFromDatabase.GetPropertyValue(FileManager.SoundTableCategoryUuidPropertyName).Split(',');
            Assert.AreEqual(updatedCategoryUuids.Count, soundTableObjectCategories.Length);
            for (int i = 0; i < updatedCategoryUuids.Count; i++)
                Assert.AreEqual(updatedCategoryUuids[i].ToString(), soundTableObjectCategories[i]);
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
        #endregion

        #region Category
        #region CreateCategoryAsync
        [TestMethod]
        public async Task CreateCategoryAsyncShouldCreateTheCategoryTableObject()
        {
            // Arrange
            Guid uuid = Guid.NewGuid();
            Guid parentUuid = Guid.NewGuid();
            string name = "Sound name";
            string icon = "icon";

            // Act
            await DatabaseOperations.CreateCategoryAsync(uuid, parentUuid, name, icon);

            // Assert
            TableObject categoryFromDatabase = await DatabaseOperations.GetTableObjectAsync(uuid);
            Assert.IsNotNull(categoryFromDatabase);
            Assert.AreEqual(uuid, categoryFromDatabase.Uuid);
            Assert.AreEqual(parentUuid.ToString(), categoryFromDatabase.GetPropertyValue(FileManager.CategoryTableParentPropertyName));
            Assert.AreEqual(name, categoryFromDatabase.GetPropertyValue(FileManager.CategoryTableNamePropertyName));
            Assert.AreEqual(icon, categoryFromDatabase.GetPropertyValue(FileManager.CategoryTableIconPropertyName));
        }

        [TestMethod]
        public async Task CreateCategoryAsyncShouldCreateTheCategoryTableObjectWithoutParent()
        {
            // Arrange
            Guid uuid = Guid.NewGuid();
            string name = "Category name";
            string icon = "icon";

            // Act
            await DatabaseOperations.CreateCategoryAsync(uuid, null, name, icon);

            // Assert
            TableObject categoryFromDatabase = await DatabaseOperations.GetTableObjectAsync(uuid);
            Assert.IsNotNull(categoryFromDatabase);
            Assert.AreEqual(uuid, categoryFromDatabase.Uuid);
            Assert.IsNull(categoryFromDatabase.GetPropertyValue(FileManager.CategoryTableParentPropertyName));
            Assert.AreEqual(name, categoryFromDatabase.GetPropertyValue(FileManager.CategoryTableNamePropertyName));
            Assert.AreEqual(icon, categoryFromDatabase.GetPropertyValue(FileManager.CategoryTableIconPropertyName));
        }
        #endregion

        #region GetAllCategoriesAsync
        [TestMethod]
        public async Task GetAllCategoriesAsyncShouldReturnAllCategories()
        {
            // Arrange
            Guid firstCategoryUuid = Guid.NewGuid();
            Guid firstCategoryParentUuid = Guid.NewGuid();
            string firstCategoryName = "First category name";
            string firstCategoryIcon = "first category icon";

            Guid secondCategoryUuid = Guid.NewGuid();
            string secondCategoryName = "Second category name";
            string secondCategoryIcon = "second category icon";

            await DatabaseOperations.CreateCategoryAsync(firstCategoryUuid, firstCategoryParentUuid, firstCategoryName, firstCategoryIcon);
            await DatabaseOperations.CreateCategoryAsync(secondCategoryUuid, null, secondCategoryName, secondCategoryIcon);

            // Act
            List<TableObject> categories = await DatabaseOperations.GetAllCategoriesAsync();

            // Assert
            Assert.AreEqual(2, categories.Count);

            Assert.AreEqual(firstCategoryUuid, categories[0].Uuid);
            Assert.AreEqual(firstCategoryParentUuid.ToString(), categories[0].GetPropertyValue(FileManager.CategoryTableParentPropertyName));
            Assert.AreEqual(firstCategoryName, categories[0].GetPropertyValue(FileManager.CategoryTableNamePropertyName));
            Assert.AreEqual(firstCategoryIcon, categories[0].GetPropertyValue(FileManager.CategoryTableIconPropertyName));

            Assert.AreEqual(secondCategoryUuid, categories[1].Uuid);
            Assert.IsNull(categories[1].GetPropertyValue(FileManager.CategoryTableParentPropertyName));
            Assert.AreEqual(secondCategoryName, categories[1].GetPropertyValue(FileManager.CategoryTableNamePropertyName));
            Assert.AreEqual(secondCategoryIcon, categories[1].GetPropertyValue(FileManager.CategoryTableIconPropertyName));
        }
        #endregion

        #region UpdateCategoryAsync
        [TestMethod]
        public async Task UpdateCategoryAsyncShouldUpdateAllValuesOfTheCategory()
        {
            // Arrange (1)
            Guid uuid = Guid.NewGuid();
            Guid parentUuid = Guid.NewGuid();
            string name = "Test-Category";
            string icon = "Test-Icon";

            // Act (1)
            await DatabaseOperations.CreateCategoryAsync(uuid, parentUuid, name, icon);

            // Assert (1)
            TableObject categoryFromDatabase = await DatabaseOperations.GetTableObjectAsync(uuid);
            Assert.IsNotNull(categoryFromDatabase);
            Assert.AreEqual(uuid, categoryFromDatabase.Uuid);
            Assert.AreEqual(parentUuid.ToString(), categoryFromDatabase.GetPropertyValue(FileManager.CategoryTableParentPropertyName));
            Assert.AreEqual(name, categoryFromDatabase.GetPropertyValue(FileManager.CategoryTableNamePropertyName));
            Assert.AreEqual(icon, categoryFromDatabase.GetPropertyValue(FileManager.CategoryTableIconPropertyName));

            // Arrange (2)
            Guid updatedParentUuid = Guid.NewGuid();
            string updatedName = "Updated category name";
            string updatedIcon = "Updated category icon";

            // Act (2)
            await DatabaseOperations.UpdateCategoryAsync(uuid, updatedParentUuid, updatedName, updatedIcon);

            // Assert (2)
            categoryFromDatabase = await DatabaseOperations.GetTableObjectAsync(uuid);
            Assert.IsNotNull(categoryFromDatabase);
            Assert.AreEqual(uuid, categoryFromDatabase.Uuid);
            Assert.AreEqual(updatedParentUuid.ToString(), categoryFromDatabase.GetPropertyValue(FileManager.CategoryTableParentPropertyName));
            Assert.AreEqual(updatedName, categoryFromDatabase.GetPropertyValue(FileManager.CategoryTableNamePropertyName));
            Assert.AreEqual(updatedIcon, categoryFromDatabase.GetPropertyValue(FileManager.CategoryTableIconPropertyName));
        }
        #endregion

        #region DeleteCategoryAsync
        [TestMethod]
        public async Task DeleteCategoryAsyncShouldDeleteTheCategory()
        {
            // Arrange
            Guid uuid = Guid.NewGuid();
            await DatabaseOperations.CreateCategoryAsync(uuid, null, "test category", "test icon");

            // Act
            await DatabaseOperations.DeleteCategoryAsync(uuid);

            // Assert
            TableObject categoryFromDatabase = await DatabaseOperations.GetTableObjectAsync(uuid);
            Assert.IsNull(categoryFromDatabase);
        }
        #endregion
        #endregion

        #region PlayingSound
        #region CreatePlayingSoundAsync
        [TestMethod]
        public async Task CreatePlayingSoundAsyncShouldCreateThePlayingSoundObjectWithTheCorrectProperties()
        {
            // Arrange
            Guid uuid = Guid.NewGuid();
            List<Guid> soundUuids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            int current = 2;
            int repetitions = 12;
            bool randomly = true;
            int volume = 78;
            bool muted = true;

            // Act
            await DatabaseOperations.CreatePlayingSoundAsync(uuid, soundUuids, current, repetitions, randomly, volume, muted);

            // Assert
            TableObject playingSoundFromDatabase = await DatabaseOperations.GetTableObjectAsync(uuid);
            Assert.IsNotNull(playingSoundFromDatabase);
            Assert.AreEqual(uuid, playingSoundFromDatabase.Uuid);
            Assert.AreEqual(current, int.Parse(playingSoundFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableCurrentPropertyName)));
            Assert.AreEqual(repetitions, int.Parse(playingSoundFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableRepetitionsPropertyName)));
            Assert.AreEqual(randomly, bool.Parse(playingSoundFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableRandomlyPropertyName)));
            Assert.AreEqual(volume, int.Parse(playingSoundFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableVolume2PropertyName)));
            Assert.AreEqual(muted, bool.Parse(playingSoundFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableMutedPropertyName)));

            string[] playingSoundTableObjectSoundUuids = playingSoundFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableSoundIdsPropertyName).Split(',');
            for (int i = 0; i < soundUuids.Count; i++)
                Assert.AreEqual(soundUuids[i].ToString(), playingSoundTableObjectSoundUuids[i]);
        }
        #endregion

        #region GetAllPlayingSoundsAsync
        [TestMethod]
        public async Task GetAllPlayingSoundsAsyncShouldReturnAllPlayingSounds()
        {
            // Arrange
            Guid firstPlayingSoundUuid = Guid.NewGuid();
            Guid secondPlayingSoundUuid = Guid.NewGuid();
            Guid thirdPlayingSoundUuid = Guid.NewGuid();

            List<Guid> firstPlayingSoundSoundUuids = new List<Guid> { Guid.NewGuid() };
            List<Guid> secondPlayingSoundSoundUuids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            List<Guid> thirdPlayingSoundSoundUuids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

            int firstPlayingSoundCurrent = 23;
            int secondPlayingSoundCurrent = 12;
            int thirdPlayingSoundCurrent = 0;

            int firstPlayingSoundRepetitions = 12;
            int secondPlayingSoundRepetitions = 2;
            int thirdPlayingSoundRepetitions = 389;

            bool firstPlayingSoundRandomly = false;
            bool secondPlayingSoundRandomly = true;
            bool thirdPlayingSoundRandomly = true;

            int firstPlayingSoundVolume = 100;
            int secondPlayingSoundVolume = 89;
            int thirdPlayingSoundVolume = 76;

            bool firstPlayingSoundMuted = false;
            bool secondPlayingSoundMuted = false;
            bool thirdPlayingSoundMuted = true;

            await DatabaseOperations.CreatePlayingSoundAsync(
                firstPlayingSoundUuid,
                firstPlayingSoundSoundUuids,
                firstPlayingSoundCurrent,
                firstPlayingSoundRepetitions,
                firstPlayingSoundRandomly,
                firstPlayingSoundVolume,
                firstPlayingSoundMuted
            );
            await DatabaseOperations.CreatePlayingSoundAsync(
                secondPlayingSoundUuid,
                secondPlayingSoundSoundUuids,
                secondPlayingSoundCurrent,
                secondPlayingSoundRepetitions,
                secondPlayingSoundRandomly,
                secondPlayingSoundVolume,
                secondPlayingSoundMuted
            );
            await DatabaseOperations.CreatePlayingSoundAsync(
                thirdPlayingSoundUuid,
                thirdPlayingSoundSoundUuids,
                thirdPlayingSoundCurrent,
                thirdPlayingSoundRepetitions,
                thirdPlayingSoundRandomly,
                thirdPlayingSoundVolume,
                thirdPlayingSoundMuted
            );

            // Act
            List<TableObject> playingSoundsFromDatabase = await DatabaseOperations.GetAllPlayingSoundsAsync();

            // Assert
            Assert.AreEqual(3, playingSoundsFromDatabase.Count);

            Assert.AreEqual(firstPlayingSoundUuid, playingSoundsFromDatabase[0].Uuid);
            Assert.AreEqual(firstPlayingSoundCurrent, int.Parse(playingSoundsFromDatabase[0].GetPropertyValue(FileManager.PlayingSoundTableCurrentPropertyName)));
            Assert.AreEqual(firstPlayingSoundRepetitions, int.Parse(playingSoundsFromDatabase[0].GetPropertyValue(FileManager.PlayingSoundTableRepetitionsPropertyName)));
            Assert.AreEqual(firstPlayingSoundRandomly, bool.Parse(playingSoundsFromDatabase[0].GetPropertyValue(FileManager.PlayingSoundTableRandomlyPropertyName)));
            Assert.AreEqual(firstPlayingSoundVolume, int.Parse(playingSoundsFromDatabase[0].GetPropertyValue(FileManager.PlayingSoundTableVolume2PropertyName)));
            Assert.AreEqual(firstPlayingSoundMuted, bool.Parse(playingSoundsFromDatabase[0].GetPropertyValue(FileManager.PlayingSoundTableMutedPropertyName)));

            string[] firstPlayingSoundTableObjectSoundUuids = playingSoundsFromDatabase[0].GetPropertyValue(FileManager.PlayingSoundTableSoundIdsPropertyName).Split(',');
            for (int i = 0; i < firstPlayingSoundSoundUuids.Count; i++)
                Assert.AreEqual(firstPlayingSoundSoundUuids[i].ToString(), firstPlayingSoundTableObjectSoundUuids[i]);

            Assert.AreEqual(secondPlayingSoundUuid, playingSoundsFromDatabase[1].Uuid);
            Assert.AreEqual(secondPlayingSoundCurrent, int.Parse(playingSoundsFromDatabase[1].GetPropertyValue(FileManager.PlayingSoundTableCurrentPropertyName)));
            Assert.AreEqual(secondPlayingSoundRepetitions, int.Parse(playingSoundsFromDatabase[1].GetPropertyValue(FileManager.PlayingSoundTableRepetitionsPropertyName)));
            Assert.AreEqual(secondPlayingSoundRandomly, bool.Parse(playingSoundsFromDatabase[1].GetPropertyValue(FileManager.PlayingSoundTableRandomlyPropertyName)));
            Assert.AreEqual(secondPlayingSoundVolume, int.Parse(playingSoundsFromDatabase[1].GetPropertyValue(FileManager.PlayingSoundTableVolume2PropertyName)));
            Assert.AreEqual(secondPlayingSoundMuted, bool.Parse(playingSoundsFromDatabase[1].GetPropertyValue(FileManager.PlayingSoundTableMutedPropertyName)));

            string[] secondPlayingSoundTableObjectSoundUuids = playingSoundsFromDatabase[1].GetPropertyValue(FileManager.PlayingSoundTableSoundIdsPropertyName).Split(',');
            for (int i = 0; i < secondPlayingSoundSoundUuids.Count; i++)
                Assert.AreEqual(secondPlayingSoundSoundUuids[i].ToString(), secondPlayingSoundTableObjectSoundUuids[i]);

            Assert.AreEqual(thirdPlayingSoundUuid, playingSoundsFromDatabase[2].Uuid);
            Assert.AreEqual(thirdPlayingSoundCurrent, int.Parse(playingSoundsFromDatabase[2].GetPropertyValue(FileManager.PlayingSoundTableCurrentPropertyName)));
            Assert.AreEqual(thirdPlayingSoundRepetitions, int.Parse(playingSoundsFromDatabase[2].GetPropertyValue(FileManager.PlayingSoundTableRepetitionsPropertyName)));
            Assert.AreEqual(thirdPlayingSoundRandomly, bool.Parse(playingSoundsFromDatabase[2].GetPropertyValue(FileManager.PlayingSoundTableRandomlyPropertyName)));
            Assert.AreEqual(thirdPlayingSoundVolume, int.Parse(playingSoundsFromDatabase[2].GetPropertyValue(FileManager.PlayingSoundTableVolume2PropertyName)));
            Assert.AreEqual(thirdPlayingSoundMuted, bool.Parse(playingSoundsFromDatabase[2].GetPropertyValue(FileManager.PlayingSoundTableMutedPropertyName)));

            string[] thirdPlayingSoundTableObjectSoundUuids = playingSoundsFromDatabase[2].GetPropertyValue(FileManager.PlayingSoundTableSoundIdsPropertyName).Split(',');
            for (int i = 0; i < thirdPlayingSoundSoundUuids.Count; i++)
                Assert.AreEqual(thirdPlayingSoundSoundUuids[i].ToString(), thirdPlayingSoundTableObjectSoundUuids[i]);
        }
        #endregion

        #region GetPlayingSoundAsync
        [TestMethod]
        public async Task GetPlayingSoundAsyncShouldReturnThePlayingSound()
        {
            // Arrange
            Guid uuid = Guid.NewGuid();
            List<Guid> soundUuids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            int current = 2;
            int repetitions = 12;
            bool randomly = true;
            int volume = 78;
            bool muted = true;

            await DatabaseOperations.CreatePlayingSoundAsync(uuid, soundUuids, current, repetitions, randomly, volume, muted);

            // Act
            TableObject playingSoundFromDatabase = await DatabaseOperations.GetPlayingSoundAsync(uuid);

            // Assert
            Assert.IsNotNull(playingSoundFromDatabase);
            Assert.AreEqual(uuid, playingSoundFromDatabase.Uuid);
            Assert.AreEqual(current, int.Parse(playingSoundFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableCurrentPropertyName)));
            Assert.AreEqual(repetitions, int.Parse(playingSoundFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableRepetitionsPropertyName)));
            Assert.AreEqual(randomly, bool.Parse(playingSoundFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableRandomlyPropertyName)));
            Assert.AreEqual(volume, int.Parse(playingSoundFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableVolume2PropertyName)));
            Assert.AreEqual(muted, bool.Parse(playingSoundFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableMutedPropertyName)));

            string[] playingSoundTableObjectSoundUuids = playingSoundFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableSoundIdsPropertyName).Split(',');
            for (int i = 0; i < soundUuids.Count; i++)
                Assert.AreEqual(soundUuids[i].ToString(), playingSoundTableObjectSoundUuids[i]);
        }

        [TestMethod]
        public async Task GetPlayingSoundAsyncShouldReturnNullIfThePlayingSoundDoesNotExist()
        {
            // Act
            TableObject playingSoundFromDatabase = await DatabaseOperations.GetPlayingSoundAsync(Guid.NewGuid());

            // Assert
            Assert.IsNull(playingSoundFromDatabase);
        }

        [TestMethod]
        public async Task GetPlayingSoundAsyncShouldReturnNullIfTheTableObjectIsNotAPlayingSound()
        {
            // Arrange
            Guid uuid = Guid.NewGuid();
            await Dav.Database.CreateTableObjectAsync(new TableObject(uuid, 123));

            // Act
            TableObject playingSoundFromDatabase = await DatabaseOperations.GetPlayingSoundAsync(uuid);

            // Assert
            Assert.IsNull(playingSoundFromDatabase);
        }
        #endregion

        #region UpdatePlayingSoundAsync
        [TestMethod]
        public async Task UpdatePlayingSoundAsyncShouldUpdateAllValuesOfThePlayingSound()
        {
            // Arrange (1)
            Guid uuid = Guid.NewGuid();
            List<Guid> soundUuids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            int current = 23;
            int repetitions = 10;
            bool randomly = true;
            int volume = 80;
            bool muted = false;

            // Act (1)
            await DatabaseOperations.CreatePlayingSoundAsync(
                uuid,
                soundUuids,
                current,
                repetitions,
                randomly,
                volume,
                muted
            );

            // Assert (1)
            TableObject playingSoundFromDatabase = await DatabaseOperations.GetTableObjectAsync(uuid);
            Assert.IsNotNull(playingSoundFromDatabase);
            Assert.AreEqual(uuid, playingSoundFromDatabase.Uuid);
            Assert.AreEqual(current, int.Parse(playingSoundFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableCurrentPropertyName)));
            Assert.AreEqual(repetitions, int.Parse(playingSoundFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableRepetitionsPropertyName)));
            Assert.AreEqual(randomly, bool.Parse(playingSoundFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableRandomlyPropertyName)));
            Assert.AreEqual(volume, int.Parse(playingSoundFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableVolume2PropertyName)));
            Assert.AreEqual(muted, bool.Parse(playingSoundFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableMutedPropertyName)));

            string[] playingSoundTableObjectSoundUuids = playingSoundFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableSoundIdsPropertyName).Split(',');
            for (int i = 0; i < soundUuids.Count; i++)
                Assert.AreEqual(soundUuids[i].ToString(), playingSoundTableObjectSoundUuids[i]);

            // Arrange (2)
            List<Guid> updatedSoundUuids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            int updatedCurrent = 3;
            int updatedRepetitions = 29;
            bool updatedRandomly = false;
            int updatedVolume = 43;
            bool updatedMuted = true;

            // Act (2)
            await DatabaseOperations.UpdatePlayingSoundAsync(uuid,
                updatedSoundUuids,
                updatedCurrent,
                updatedRepetitions,
                updatedRandomly,
                updatedVolume,
                updatedMuted
            );

            // Assert (2)
            playingSoundFromDatabase = await DatabaseOperations.GetTableObjectAsync(uuid);
            Assert.IsNotNull(playingSoundFromDatabase);
            Assert.AreEqual(uuid, playingSoundFromDatabase.Uuid);
            Assert.AreEqual(updatedCurrent, int.Parse(playingSoundFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableCurrentPropertyName)));
            Assert.AreEqual(updatedRepetitions, int.Parse(playingSoundFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableRepetitionsPropertyName)));
            Assert.AreEqual(updatedRandomly, bool.Parse(playingSoundFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableRandomlyPropertyName)));
            Assert.AreEqual(updatedVolume, int.Parse(playingSoundFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableVolume2PropertyName)));
            Assert.AreEqual(updatedMuted, bool.Parse(playingSoundFromDatabase.GetPropertyValue(FileManager.PlayingSoundTableMutedPropertyName)));
        }
        #endregion
        #endregion

        #region Order
        [TestMethod]
        public async Task GetAllOrdersAsyncShouldReturnAllOrders()
        {
            // Arrange
            Guid categoryOrderParentUuid = Guid.NewGuid();
            List<Guid> categoryOrderUuids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

            Guid soundOrderCategoryUuid = Guid.NewGuid();
            bool soundOrderFavourite = false;
            List<Guid> soundOrderUuids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

            await DatabaseOperations.SetCategoryOrderAsync(categoryOrderParentUuid, categoryOrderUuids);
            await DatabaseOperations.SetSoundOrderAsync(soundOrderCategoryUuid, soundOrderFavourite, soundOrderUuids);

            // Act
            List<TableObject> ordersFromDatabase = await DatabaseOperations.GetAllOrdersAsync();

            // Assert
            Assert.AreEqual(2, ordersFromDatabase.Count);

            Assert.AreEqual(FileManager.CategoryOrderType, ordersFromDatabase[0].GetPropertyValue(FileManager.OrderTableTypePropertyName));
            Assert.AreEqual(categoryOrderUuids[0].ToString(), ordersFromDatabase[0].GetPropertyValue("0"));
            Assert.AreEqual(categoryOrderUuids[1].ToString(), ordersFromDatabase[0].GetPropertyValue("1"));
            Assert.IsNull(ordersFromDatabase[0].GetPropertyValue("2"));

            Assert.AreEqual(FileManager.SoundOrderType, ordersFromDatabase[1].GetPropertyValue(FileManager.OrderTableTypePropertyName));
            Assert.AreEqual(soundOrderFavourite, bool.Parse(ordersFromDatabase[1].GetPropertyValue(FileManager.OrderTableFavouritePropertyName)));
            Assert.AreEqual(soundOrderUuids[0].ToString(), ordersFromDatabase[1].GetPropertyValue("0"));
            Assert.AreEqual(soundOrderUuids[1].ToString(), ordersFromDatabase[1].GetPropertyValue("1"));
            Assert.AreEqual(soundOrderUuids[2].ToString(), ordersFromDatabase[1].GetPropertyValue("2"));
            Assert.IsNull(ordersFromDatabase[1].GetPropertyValue("3"));
        }
        #endregion

        #region CategoryOrder
        #region SetCategoryOrderAsync
        [TestMethod]
        public async Task SetCategoryOrderAsyncShouldCreateNewCategoryOrder()
        {
            // Arrange
            Guid parentUuid = Guid.NewGuid();
            List<Guid> uuids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

            // Act
            await DatabaseOperations.SetCategoryOrderAsync(parentUuid, uuids);

            // Assert
            List<TableObject> ordersFromDatabase = await DatabaseOperations.GetAllOrdersAsync();
            Assert.AreEqual(1, ordersFromDatabase.Count);
            Assert.AreEqual(FileManager.CategoryOrderType, ordersFromDatabase[0].GetPropertyValue(FileManager.OrderTableTypePropertyName));
            Assert.AreEqual(uuids[0].ToString(), ordersFromDatabase[0].GetPropertyValue("0"));
            Assert.AreEqual(uuids[1].ToString(), ordersFromDatabase[0].GetPropertyValue("1"));
            Assert.AreEqual(uuids[2].ToString(), ordersFromDatabase[0].GetPropertyValue("2"));
            Assert.IsNull(ordersFromDatabase[0].GetPropertyValue("3"));
        }

        [TestMethod]
        public async Task SetCategoryOrderAsyncShouldUpdateExistingOrder()
        {
            // Arrange (1)
            Guid parentUuid = Guid.NewGuid();
            List<Guid> uuids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

            // Act (1)
            await DatabaseOperations.SetCategoryOrderAsync(parentUuid, uuids);

            // Assert (1)
            List<TableObject> ordersFromDatabase = await DatabaseOperations.GetAllOrdersAsync();
            Assert.AreEqual(1, ordersFromDatabase.Count);
            Assert.AreEqual(FileManager.CategoryOrderType, ordersFromDatabase[0].GetPropertyValue(FileManager.OrderTableTypePropertyName));
            Assert.AreEqual(uuids[0].ToString(), ordersFromDatabase[0].GetPropertyValue("0"));
            Assert.AreEqual(uuids[1].ToString(), ordersFromDatabase[0].GetPropertyValue("1"));
            Assert.AreEqual(uuids[2].ToString(), ordersFromDatabase[0].GetPropertyValue("2"));
            Assert.IsNull(ordersFromDatabase[0].GetPropertyValue("3"));

            // Arrange (2)
            List<Guid> updatedUuids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

            // Act (2)
            await DatabaseOperations.SetCategoryOrderAsync(parentUuid, updatedUuids);

            // Assert (2)
            ordersFromDatabase = await DatabaseOperations.GetAllOrdersAsync();
            Assert.AreEqual(1, ordersFromDatabase.Count);
            Assert.AreEqual(FileManager.CategoryOrderType, ordersFromDatabase[0].GetPropertyValue(FileManager.OrderTableTypePropertyName));
            Assert.AreEqual(updatedUuids[0].ToString(), ordersFromDatabase[0].GetPropertyValue("0"));
            Assert.AreEqual(updatedUuids[1].ToString(), ordersFromDatabase[0].GetPropertyValue("1"));
            Assert.IsNull(ordersFromDatabase[0].GetPropertyValue("2"));
        }
        #endregion

        #region DeleteCategoryOrderAsync
        [TestMethod]
        public async Task DeleteCategoryOrderAsyncShouldDeleteTheCategoryOrder()
        {
            // Arrange (1)
            Guid parentUuid = Guid.NewGuid();
            List<Guid> uuids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

            // Act (1)
            await DatabaseOperations.SetCategoryOrderAsync(parentUuid, uuids);

            // Assert (1)
            List<TableObject> ordersFromDatabase = await DatabaseOperations.GetAllOrdersAsync();
            Assert.AreEqual(1, ordersFromDatabase.Count);
            Assert.AreEqual(FileManager.CategoryOrderType, ordersFromDatabase[0].GetPropertyValue(FileManager.OrderTableTypePropertyName));
            Assert.AreEqual(uuids[0].ToString(), ordersFromDatabase[0].GetPropertyValue("0"));
            Assert.AreEqual(uuids[1].ToString(), ordersFromDatabase[0].GetPropertyValue("1"));
            Assert.AreEqual(uuids[2].ToString(), ordersFromDatabase[0].GetPropertyValue("2"));
            Assert.IsNull(ordersFromDatabase[0].GetPropertyValue("3"));

            // Act (2)
            await DatabaseOperations.DeleteCategoryOrderAsync(parentUuid);

            // Assert (2)
            ordersFromDatabase = await DatabaseOperations.GetAllOrdersAsync();
            Assert.AreEqual(0, ordersFromDatabase.Count);
        }
        #endregion
        #endregion

        #region SoundOrder
        #region SetSoundOrderAsync
        [TestMethod]
        public async Task SetSoundOrderAsyncShouldCreateNewSoundOrder()
        {
            // Arrange
            Guid categoryUuid = Guid.NewGuid();
            bool favourite = true;
            List<Guid> uuids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

            // Act
            await DatabaseOperations.SetSoundOrderAsync(categoryUuid, favourite, uuids);

            // Assert
            List<TableObject> ordersFromDatabase = await DatabaseOperations.GetAllOrdersAsync();
            Assert.AreEqual(1, ordersFromDatabase.Count);
            Assert.AreEqual(FileManager.SoundOrderType, ordersFromDatabase[0].GetPropertyValue(FileManager.OrderTableTypePropertyName));
            Assert.AreEqual(favourite, bool.Parse(ordersFromDatabase[0].GetPropertyValue(FileManager.OrderTableFavouritePropertyName)));
            Assert.AreEqual(uuids[0].ToString(), ordersFromDatabase[0].GetPropertyValue("0"));
            Assert.AreEqual(uuids[1].ToString(), ordersFromDatabase[0].GetPropertyValue("1"));
            Assert.AreEqual(uuids[2].ToString(), ordersFromDatabase[0].GetPropertyValue("2"));
            Assert.IsNull(ordersFromDatabase[0].GetPropertyValue("3"));
        }

        [TestMethod]
        public async Task SetSoundOrderAsyncShouldUpdateExistingOrder()
        {
            // Arrange (1)
            Guid categoryUuid = Guid.NewGuid();
            bool favourite = true;
            List<Guid> uuids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

            // Act (1)
            await DatabaseOperations.SetSoundOrderAsync(categoryUuid, favourite, uuids);

            // Assert (1)
            List<TableObject> ordersFromDatabase = await DatabaseOperations.GetAllOrdersAsync();
            Assert.AreEqual(1, ordersFromDatabase.Count);
            Assert.AreEqual(FileManager.SoundOrderType, ordersFromDatabase[0].GetPropertyValue(FileManager.OrderTableTypePropertyName));
            Assert.AreEqual(favourite, bool.Parse(ordersFromDatabase[0].GetPropertyValue(FileManager.OrderTableFavouritePropertyName)));
            Assert.AreEqual(uuids[0].ToString(), ordersFromDatabase[0].GetPropertyValue("0"));
            Assert.AreEqual(uuids[1].ToString(), ordersFromDatabase[0].GetPropertyValue("1"));
            Assert.AreEqual(uuids[2].ToString(), ordersFromDatabase[0].GetPropertyValue("2"));
            Assert.IsNull(ordersFromDatabase[0].GetPropertyValue("3"));

            // Arrange (2)
            List<Guid> updatedUuids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

            // Act (2)
            await DatabaseOperations.SetSoundOrderAsync(categoryUuid, favourite, updatedUuids);

            // Assert (2)
            ordersFromDatabase = await DatabaseOperations.GetAllOrdersAsync();
            Assert.AreEqual(1, ordersFromDatabase.Count);
            Assert.AreEqual(FileManager.SoundOrderType, ordersFromDatabase[0].GetPropertyValue(FileManager.OrderTableTypePropertyName));
            Assert.AreEqual(favourite, bool.Parse(ordersFromDatabase[0].GetPropertyValue(FileManager.OrderTableFavouritePropertyName)));
            Assert.AreEqual(updatedUuids[0].ToString(), ordersFromDatabase[0].GetPropertyValue("0"));
            Assert.AreEqual(updatedUuids[1].ToString(), ordersFromDatabase[0].GetPropertyValue("1"));
            Assert.IsNull(ordersFromDatabase[0].GetPropertyValue("2"));
        }
        #endregion

        #region DeleteSoundOrderAsync
        [TestMethod]
        public async Task DeleteSoundOrderAsyncShouldDeleteTheSoundOrder()
        {
            // Arrange (1)
            Guid categoryUuid = Guid.NewGuid();
            bool favourite = true;
            List<Guid> uuids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

            // Act (1)
            await DatabaseOperations.SetSoundOrderAsync(categoryUuid, favourite, uuids);

            // Assert (1)
            List<TableObject> ordersFromDatabase = await DatabaseOperations.GetAllOrdersAsync();
            Assert.AreEqual(1, ordersFromDatabase.Count);
            Assert.AreEqual(FileManager.SoundOrderType, ordersFromDatabase[0].GetPropertyValue(FileManager.OrderTableTypePropertyName));
            Assert.AreEqual(uuids[0].ToString(), ordersFromDatabase[0].GetPropertyValue("0"));
            Assert.AreEqual(uuids[1].ToString(), ordersFromDatabase[0].GetPropertyValue("1"));
            Assert.AreEqual(uuids[2].ToString(), ordersFromDatabase[0].GetPropertyValue("2"));
            Assert.IsNull(ordersFromDatabase[0].GetPropertyValue("3"));

            // Act (2)
            await DatabaseOperations.DeleteSoundOrderAsync(categoryUuid, favourite);

            // Assert (2)
            ordersFromDatabase = await DatabaseOperations.GetAllOrdersAsync();
            Assert.AreEqual(0, ordersFromDatabase.Count);
        }
        #endregion
        #endregion
    }
}

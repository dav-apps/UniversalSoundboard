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
        public async Task AddSoundShouldCreateSoundObjectWithTheCorrectProperties()
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
        public async Task AddSoundShouldCreateSoundObjectWithoutCategories()
        {
            // Arrange
            var uuid = Guid.NewGuid();
            string soundFileUuid = Guid.NewGuid().ToString();
            string name = "Godot Objection";

            // Act
            await DatabaseOperations.AddSoundAsync(uuid, name, soundFileUuid, null);

            // Assert
            var tableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(uuid);
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
    }
}

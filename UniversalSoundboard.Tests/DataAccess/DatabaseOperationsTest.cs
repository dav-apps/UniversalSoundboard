using davClassLibrary;
using davClassLibrary.Common;
using davClassLibrary.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniversalSoundboard.Tests.Common;
using UniversalSoundBoard.DataAccess;

namespace UniversalSoundboard.Tests.DataAccess
{
    [TestClass]
    public class DatabaseOperationsTest
    {
        [ClassInitialize]
        public static void Setup(TestContext context)
        {
            ProjectInterface.RetrieveConstants = new RetrieveConstants();
            ProjectInterface.GeneralMethods = new GeneralMethods();
            ProjectInterface.LocalDataSettings = new LocalDataSettings();
            ProjectInterface.TriggerAction = new TriggerAction();
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
    }
}

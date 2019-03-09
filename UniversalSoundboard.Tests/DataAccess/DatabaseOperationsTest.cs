using davClassLibrary.Common;
using davClassLibrary.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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
    }
}

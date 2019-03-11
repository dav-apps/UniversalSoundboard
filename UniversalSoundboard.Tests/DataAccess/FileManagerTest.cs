using davClassLibrary.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UniversalSoundboard.Tests.Common;
using UniversalSoundBoard.DataAccess;
using UniversalSoundBoard.Models;

namespace UniversalSoundboard.Tests.DataAccess
{
    [TestClass][DoNotParallelize]
    public class FileManagerTest
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
            FileManager.itemViewHolder.User = new davClassLibrary.Models.DavUser();
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

        #region SortSoundsListByCustomOrder
        [TestMethod]
        public async Task SortSoundsListShouldCreateNewOrder()
        {
            // Arrange
            Guid categoryUuid = Guid.NewGuid();
            List<Sound> sounds = new List<Sound>
            {
                new Sound(Guid.NewGuid()),
                new Sound(Guid.NewGuid()),
                new Sound(Guid.NewGuid())
            };

            // Act
            List<Sound> sortedSounds = await FileManager.SortSoundsListByCustomOrderAsync(sounds, categoryUuid, false);

            // Assert
            var orders = await DatabaseOperations.GetAllOrdersAsync();
            Assert.AreEqual(1, orders.Count);
            var soundOrder = orders[0];

            int i = 0;
            foreach(var sound in sounds)
            {
                Assert.AreEqual(sound.Uuid, sortedSounds[i].Uuid);
                Assert.AreEqual(sound.Uuid, Guid.Parse(soundOrder.GetPropertyValue(i.ToString())));
                i++;
            }
        }

        [TestMethod]
        public async Task SortSoundsListByCustomOrderShouldSortTheListInTheCorrectOrder()
        {
            // Arrange
            Guid categoryUuid = Guid.NewGuid();
            List<Sound> soundsInCorrectOrder = new List<Sound>
            {
                new Sound(Guid.NewGuid()),
                new Sound(Guid.NewGuid()),
                new Sound(Guid.NewGuid())
            };

            // Create the sound order
            await FileManager.SortSoundsListByCustomOrderAsync(soundsInCorrectOrder, categoryUuid, false);

            // Create sounds list with the same sounds in another order
            List<Sound> soundsInIncorrectOrder = new List<Sound>
            {
                soundsInCorrectOrder[2],
                soundsInCorrectOrder[0],
                soundsInCorrectOrder[1]
            };

            // Act
            List<Sound> sortedSounds = await FileManager.SortSoundsListByCustomOrderAsync(soundsInIncorrectOrder, categoryUuid, false);

            // Assert
            var orders = await DatabaseOperations.GetAllOrdersAsync();
            Assert.AreEqual(1, orders.Count);
            var soundOrder = orders[0];

            // The sorted sounds should be in the same order as soundsInCorrectOrder
            int i = 0;
            foreach (var sound in soundsInCorrectOrder)
            {
                Assert.AreEqual(sound.Uuid, sortedSounds[i].Uuid);
                Assert.AreEqual(sound.Uuid, Guid.Parse(soundOrder.GetPropertyValue(i.ToString())));
                i++;
            }
        }

        [TestMethod]
        public async Task SortSoundsListShouldSaveNewSoundsAtTheEndOfTheOrder()
        {
            // Arrange
            Guid categoryUuid = Guid.NewGuid();
            List<Sound> sounds = new List<Sound>
            {
                new Sound(Guid.NewGuid()),
                new Sound(Guid.NewGuid()),
                new Sound(Guid.NewGuid())
            };

            // Create the sound order
            await FileManager.SortSoundsListByCustomOrderAsync(sounds, categoryUuid, false);

            // Add some sounds to the list
            sounds.Add(new Sound(Guid.NewGuid()));
            sounds.Add(new Sound(Guid.NewGuid()));

            // Act
            List<Sound> sortedSounds = await FileManager.SortSoundsListByCustomOrderAsync(sounds, categoryUuid, false);

            // Assert
            var orders = await DatabaseOperations.GetAllOrdersAsync();
            Assert.AreEqual(1, orders.Count);
            var soundOrder = orders[0];

            int i = 0;
            foreach (var sound in sounds)
            {
                Assert.AreEqual(sound.Uuid, sortedSounds[i].Uuid);
                Assert.AreEqual(sound.Uuid, Guid.Parse(soundOrder.GetPropertyValue(i.ToString())));
                i++;
            }
        }

        [TestMethod]
        public async Task SortSoundsListShouldMergeMultipleOrdersInTheCorrectOrder()
        {
            // Arrange
            Guid categoryUuid = Guid.NewGuid();
            List<Sound> firstSoundsList = new List<Sound>
            {
                new Sound(Guid.NewGuid()),
                new Sound(Guid.NewGuid()),
                new Sound(Guid.NewGuid())
            };
            List<Sound> secondSoundsList = new List<Sound>
            {
                new Sound(Guid.NewGuid()),
                new Sound(Guid.NewGuid()),
                new Sound(Guid.NewGuid())
            };

            // Create two sound orders
            await FileManager.SortSoundsListByCustomOrderAsync(firstSoundsList, Guid.NewGuid(), false);
            await FileManager.SortSoundsListByCustomOrderAsync(secondSoundsList, Guid.NewGuid(), false);

            // Get the orders and set the category uuid
            var orders = await DatabaseOperations.GetAllOrdersAsync();
            Assert.AreEqual(2, orders.Count);

            await orders[0].SetPropertyValueAsync(FileManager.OrderTableCategoryPropertyName, categoryUuid.ToString());
            await orders[1].SetPropertyValueAsync(FileManager.OrderTableCategoryPropertyName, categoryUuid.ToString());

            // Act
            List<Sound> sortedSounds = await FileManager.SortSoundsListByCustomOrderAsync(secondSoundsList, categoryUuid, false);

            // Assert
            orders = await DatabaseOperations.GetAllOrdersAsync();
            Assert.AreEqual(1, orders.Count);
            var soundOrder = orders[0];

            // Check if sortedSounds has the correct order
            int i = 0;
            foreach (var sound in secondSoundsList)
            {
                Assert.AreEqual(sound.Uuid, sortedSounds[i].Uuid);
                i++;
            }

            // Check if the order contains all sounds in the correct order
            i = 0;
            foreach(var sound in secondSoundsList)
            {
                Assert.AreEqual(sound.Uuid, Guid.Parse(soundOrder.GetPropertyValue(i.ToString())));
                i++;
            }

            foreach(var sound in firstSoundsList)
            {
                Assert.AreEqual(sound.Uuid, Guid.Parse(soundOrder.GetPropertyValue(i.ToString())));
                i++;
            }
        }
        #endregion
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UniversalSoundboard.Models;
using UniversalSoundBoard;
using UniversalSoundBoard.DataAccess;
using UniversalSoundBoard.Models;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Networking.BackgroundTransfer;
using Windows.Security.Authentication.Web;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using static UniversalSoundBoard.Models.SyncObject;

namespace UniversalSoundboard.DataAccess
{
    public class ApiManager
    {
        private const string ApiBaseUrl = "http://localhost:3111/v1/";
        private const int AppId = 8;
        private const string SoundFileTableName = "SoundFile";
        private const string ImageFileTableName = "ImageFile";
        private const string CategoryTableName = "Category";
        private const string SoundTableName = "Sound";
        private const string PlayingSoundTableName = "PlayingSound";

        /*
        private static async Task UploadData()
        {
            // Add all categories to the SyncCategory table
            for(int i = 1; i < (App.Current as App)._itemViewHolder.categories.Count; i++)
            {
                
                DatabaseOperations.AddSyncObject(SyncTable.SyncCategory, 
                                                Guid.Parse((App.Current as App)._itemViewHolder.categories[i].Uuid), 
                                                SyncOperation.Create);
                                                
            }

            
            foreach(Sound sound in (App.Current as App)._itemViewHolder.sounds)
            {
                DatabaseOperations.AddSyncObject(SyncTable.SyncSound, Guid.Parse(sound.Uuid), SyncOperation.Create);
            }
            
            //DatabaseOperations.AddSyncObject(SyncTable.SyncSound, Guid.Parse((App.Current as App)._itemViewHolder.sounds[1].Uuid), SyncOperation.Create);

            await SyncSoundboard();
        }
        */
        /*
        private static async Task<BitmapImage> GetAvatar()
        {
            BitmapImage image = new BitmapImage();
            image.UriSource = new Uri("ms-appx:///Assets/Images/avatar-default.png");

            StorageFolder userFolder = await FileManager.GetUserFolderAsnyc();
            StorageFile avatar = await userFolder.TryGetItemAsync("avatar.png") as StorageFile;

            if(avatar != null)
            {
                image.UriSource = new Uri(avatar.Path);
            }

            return image;
        }
        */
        /*
        public static async Task SyncSoundboard()
        {
            // Upload the categories
            // Get all SyncCategory entries and apply the changes
            List<SyncObject> syncCategories = DatabaseOperations.GetAllSyncObjects(SyncTable.SyncCategory);
            foreach(SyncObject syncObject in syncCategories)
            {
                await UploadCategory(syncObject.Id, syncObject.Uuid, syncObject.Operation);
            }

            // Upload the sounds
            List<SyncObject> syncSounds = DatabaseOperations.GetAllSyncObjects(SyncTable.SyncSound);
            foreach(SyncObject syncObject in syncSounds)
            {
                UploadSound(syncObject.Id, syncObject.Uuid, syncObject.Operation);
            }

            // Upload the playingSounds
            List<SyncObject> syncPlayingSounds = DatabaseOperations.GetAllSyncObjects(SyncTable.SyncPlayingSound);
            foreach(SyncObject syncObject in syncPlayingSounds)
            {
                UploadPlayingSound(syncObject.Id, syncObject.Uuid, syncObject.Operation);
            }
        }
        */
        #region Upload Category changes
            /*
        private static async Task UploadCategory(int id, Guid uuid, SyncOperation syncOperation)
        {
            // Get the category from the database
            Category category = DatabaseOperations.GetCategory(uuid.ToString());

            HttpClient httpClient = new HttpClient();

            Uri requestUri;
            HttpMethod httpMethod;

            if (syncOperation == SyncOperation.Create)
            {
                requestUri = new Uri(ApiBaseUrl +
                                    "apps/object?table_name=" + CategoryTableName +
                                    "&app_id=" + AppId.ToString() +
                                    "&uuid=" + uuid);
                httpMethod = HttpMethod.Post;
            }
            else if (syncOperation == SyncOperation.Update)
            {
                requestUri = new Uri(ApiBaseUrl + "apps/object/" + uuid.ToString());
                httpMethod = HttpMethod.Put;
            }
            else
            {
                requestUri = new Uri(ApiBaseUrl + "apps/object/" + uuid.ToString());
                httpMethod = HttpMethod.Delete;
            }

            string content = JsonConvert.SerializeObject(new { name = category.Name, icon = category.Icon });

            HttpRequestMessage requestMessage = new HttpRequestMessage(httpMethod, requestUri);
            requestMessage.Content = new StringContent(content, Encoding.UTF8, "application/json");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue(GetJwt());

            HttpResponseMessage response = await httpClient.SendAsync(requestMessage);
            string httpResponseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                // Remove the SyncObject from the database
                //DatabaseOperations.DeleteSyncObject(SyncTable.SyncCategory, id);
            }
            else
            {
                // TODO Check if the object already exists
                Debug.WriteLine("Error in UploadCategory");
                Debug.WriteLine(httpResponseBody);

                // Error code 2704: uuid already taken
                if (httpResponseBody.Contains("2704"))
                {
                    // Remove the object from the database
                    //DatabaseOperations.DeleteSyncObject(SyncTable.SyncCategory, id);
                }
            }
        }
        */
        #endregion
        /*
        #region Upload Sound changes
        private static void UploadSound(int id, Guid uuid, SyncOperation syncOperation)
        {
            
        }
        #endregion

        #region Upload PlayingSound changes
        private static void UploadPlayingSound(int id, Guid uuid, SyncOperation syncOperation)
        {

        }
        #endregion
    */
    }
}

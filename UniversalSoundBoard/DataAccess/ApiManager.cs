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
using Windows.Networking.BackgroundTransfer;
using Windows.Security.Authentication.Web;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using static UniversalSoundBoard.Models.SyncObject;

namespace UniversalSoundboard.DataAccess
{
    public class ApiManager
    {
        //private const string ApiBaseUrl = "https://dav-backend.herokuapp.com/v1/";
        private const string ApiBaseUrl = "http://localhost:3111/v1/";
        //private const int AppId = 8;
        private const int AppId = 8;
        private const string SoundFileTableName = "SoundFile";
        private const string ImageFileTableName = "ImageFile";
        private const string CategoryTableName = "Category";
        private const string SoundTableName = "Sound";
        private const string PlayingSoundTableName = "PlayingSound";

        private static string jwt = "";

        public static async Task<User> GetUser()
        {
            if(String.IsNullOrEmpty(GetJwt()))
            {
                return null;
            }

            if (NetworkInterface.GetIsNetworkAvailable())
            {
                HttpClient httpClient = new HttpClient();
                var headers = httpClient.DefaultRequestHeaders;
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(GetJwt());

                Uri requestUri = new Uri(ApiBaseUrl + "auth/user");

                HttpResponseMessage httpResponse = new HttpResponseMessage();
                string httpResponseBody = "";

                try
                {
                    //Send the GET request
                    httpResponse = await httpClient.GetAsync(requestUri);
                    httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        // Deserialize the json and create a user object
                        var serializer = new DataContractJsonSerializer(typeof(UserData));
                        var ms = new MemoryStream(Encoding.UTF8.GetBytes(httpResponseBody));
                        var dataReader = (UserData)serializer.ReadObject(ms);
                        User user = new User(dataReader.username, dataReader.total_storage, dataReader.used_storage);

                        // If the etag is outdated, download the avatar
                        if (!CheckAvatarEtag(dataReader.avatar_etag))
                        {
                            DownloadAvatar(dataReader.avatar);
                            SetAvatarEtag(dataReader.avatar_etag);
                        }

                        // Get the avatar of the user
                        user.Avatar = await GetAvatar();

                        SetUserInLocalSettings(user.Username, user.TotalStorage, user.UsedStorage);
                        return user;
                    }
                    else
                    {
                        // Clear the JWT if it is invalid or expired
                        Debug.WriteLine(httpResponse.StatusCode);
                        Debug.WriteLine(httpResponse.Content);
                    }
                }
                catch (Exception ex)
                {
                    httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
                    Debug.WriteLine(httpResponseBody);
                }
            }
            else
            {
                return await GetUserFromLocalSettings();
            }
            return null;
        }

        private static async Task<User> GetUserFromLocalSettings()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            ApplicationDataCompositeValue composite = (ApplicationDataCompositeValue)localSettings.Values[FileManager.userKey];

            if (composite != null)
            {
                string username = composite[FileManager.userUsernameKey] as string;
                var totalStorageObject = composite[FileManager.userTotalStorageKey];
                var usedStorageObject = composite[FileManager.userUsedStorageKey];
                long totalStorage = 0;
                long usedStorage = 0;

                if (totalStorageObject != null)
                    totalStorage = long.Parse(totalStorageObject.ToString());

                if (usedStorageObject != null)
                    usedStorage = long.Parse(usedStorageObject.ToString());

                if(!String.IsNullOrEmpty(username) && totalStorageObject != null && usedStorageObject != null)
                {
                    User user = new User(username, totalStorage, usedStorage);
                    user.Avatar = await GetAvatar();
                    return user;
                }
            }
            return null;
        }

        public static async Task Login()
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                Uri redirectUrl = WebAuthenticationBroker.GetCurrentApplicationCallbackUri();
                string apiKey = "gHgHKRbIjdguCM4cv5481hdiF5hZGWZ4x12Ur-7v";
                Uri requestUrl = new Uri("https://dav-apps.tech/login_implicit?api_key=" + apiKey + "&redirect_url=" + redirectUrl);

                var webAuthenticationResult = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, requestUrl);
                switch (webAuthenticationResult.ResponseStatus)
                {
                    case WebAuthenticationStatus.Success:
                        // Get the JWT from the response string
                        string jwt = webAuthenticationResult.ResponseData.Split(new[] { "jwt=" }, StringSplitOptions.None)[1];
                        SetJwt(jwt);
                        await UploadData();
                        break;
                    default:
                        Debug.WriteLine("There was an error with logging you in.");
                        break;
                }
            }
            else
            {
                Debug.WriteLine("No internet connection");
            }
        }

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

            await SyncSoundboard();
        }

        public static void Logout()
        {
            SetJwt("");
        }

        private static void SetUserInLocalSettings(string username, long totalStorage, long usedStorage)
        {
            var localSettings = ApplicationData.Current.LocalSettings;

            ApplicationDataCompositeValue userComposite = new ApplicationDataCompositeValue();
            userComposite[FileManager.userUsernameKey] = username;
            userComposite[FileManager.userTotalStorageKey] = totalStorage;
            userComposite[FileManager.userUsedStorageKey] = usedStorage;

            localSettings.Values[FileManager.userKey] = userComposite;
        }

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

        private static async void DownloadAvatar(string avatarUrl)
        {
            StorageFolder userFolder = await FileManager.GetUserFolderAsnyc();
            
            using (var client = new WebClient())
            {
                client.DownloadFile(avatarUrl, userFolder.Path + "/avatar.png");
            }
        }

        private static bool CheckAvatarEtag(string etag)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            var avatarEtagObject = localSettings.Values[FileManager.avatarEtagKey];
            bool etagIsUpToDate = false;

            if(avatarEtagObject != null)
            {
                if(avatarEtagObject.ToString() == etag)
                {
                    etagIsUpToDate = true;
                }
            }

            return etagIsUpToDate;
        }

        private static void SetAvatarEtag(string etag)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[FileManager.avatarEtagKey] = etag;
        }

        public static string GetJwt()
        {
            if (String.IsNullOrEmpty(jwt))
            {
                var localSettings = ApplicationData.Current.LocalSettings;

                var jwtObject = localSettings.Values[FileManager.jwtKey];

                if (jwtObject != null)
                {
                    if (!String.IsNullOrEmpty(jwtObject.ToString()))
                    {
                        jwt = jwtObject.ToString();
                        return jwt;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return jwt;
            }
        }

        public static void SetJwt(string newJwt)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[FileManager.jwtKey] = newJwt;
            jwt = newJwt;
        }

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
                await UploadSound(syncObject.Id, syncObject.Uuid, syncObject.Operation);
            }

            // Upload the playingSounds
            List<SyncObject> syncPlayingSounds = DatabaseOperations.GetAllSyncObjects(SyncTable.SyncPlayingSound);
            foreach(SyncObject syncObject in syncPlayingSounds)
            {
                UploadPlayingSound(syncObject.Id, syncObject.Uuid, syncObject.Operation);
            }
        }

        #region Upload Category changes
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
                DatabaseOperations.DeleteSyncObject(SyncTable.SyncCategory, id);
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
                    DatabaseOperations.DeleteSyncObject(SyncTable.SyncCategory, id);
                }
            }
        }
        #endregion
        
        #region Upload Sound changes
        private static async Task UploadSound(int id, Guid uuid, SyncOperation syncOperation)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            // - Sound informationen
            // - Sound File
            // - Image File
            /*
             * 1. Sound File mit neu erstellter UUID hochladen
             * 2. sound_uuid in der DB speichern
             * 3. Sound Informationen hochladen
             * 4. Wenn es ein Bild gibt, mit neu erstellter UUID speichern
             * 5. uuid in der DB speichern
             * 6. Sound Informationen auf dem Server aktualisieren
             * 
             * */

            // 0. Get the sound from the database
            Sound sound = await FileManager.GetSound(uuid.ToString());

            // 1. Upload the SoundFile
            Guid soundFileUuid = Guid.NewGuid();

            Uri soundFileUri = new Uri(ApiBaseUrl +
                                        "apps/object?table_name=" + SoundFileTableName +
                                        "&app_id=" + AppId.ToString() +
                                        "&uuid=" + soundFileUuid +
                                        "&ext=" + sound.AudioFile.FileType.Replace(".", ""));
            BackgroundUploader uploader = new BackgroundUploader();
            uploader.SetRequestHeader("Authorization", GetJwt());
            uploader.SetRequestHeader("Content-Type", "audio/mpeg");
            UploadOperation upload = uploader.CreateUpload(soundFileUri, sound.AudioFile);

            // Start the upload
            Progress<UploadOperation> progressCallback = new Progress<UploadOperation>(UploadProgress);
            await upload.StartAsync().AsTask(cts.Token, progressCallback);
        }

        private static void UploadProgress(UploadOperation upload)
        {
            BackgroundUploadProgress currentProgress = upload.Progress;

            double percentSent = 100;
            if (currentProgress.TotalBytesToSend > 0)
            {
                percentSent = currentProgress.BytesSent * 100 / currentProgress.TotalBytesToSend;
            }

            Debug.WriteLine(String.Format("Sent bytes: {0} of {1}", currentProgress.BytesSent, currentProgress.TotalBytesToSend));
            Debug.WriteLine(String.Format("progress: {0}", percentSent));
        }
        #endregion

        #region Upload PlayingSound changes
        private static void UploadPlayingSound(int id, Guid uuid, SyncOperation syncOperation)
        {

        }
        #endregion
    }
}

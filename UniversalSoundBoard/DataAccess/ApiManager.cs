using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using UniversalSoundboard.Models;
using UniversalSoundBoard.DataAccess;
using UniversalSoundBoard.Models;
using Windows.Storage;

namespace UniversalSoundboard.DataAccess
{
    public class ApiManager
    {
        private const string ApiBaseUrl = "https://dav-backend.herokuapp.com/v1/";

        public static async Task<User> GetUser()
        {
            string jwt = GetJwt();

            if(jwt == null)
            {
                return null;
            }

            HttpClient httpClient = new HttpClient();
            var headers = httpClient.DefaultRequestHeaders;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(jwt);

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

                    // Download the avatar
                    StorageFolder userFolder = await FileManager.GetUserFolderAsnyc();


                    return user;
                }
                else
                {
                    // Clear the JWT if it is invalid or expired

                }
            }
            catch (Exception ex)
            {
                httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
                Debug.WriteLine(httpResponseBody);
            }

            return null;
        }

        public static string GetJwt()
        {
            var localSettings = ApplicationData.Current.LocalSettings;

            var jwtObject = localSettings.Values[FileManager.jwtKey];

            if (jwtObject != null)
            {
                string jwt = jwtObject.ToString();
                return !String.IsNullOrEmpty(jwt) ? jwt : null;
            }
            else
            {
                return null;
            }
        }
    }
}

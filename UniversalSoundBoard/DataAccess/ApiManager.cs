using davClassLibrary;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using UniversalSoundboard.Models;
using Windows.Storage;

namespace UniversalSoundboard.DataAccess
{
    public class ApiManager
    {
        private static HttpClient httpClient;
        public static HttpClient HttpClient
        {
            get
            {
                if (httpClient == null)
                {
                    httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(60) };
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Dav.AccessToken);
                }

                return httpClient;
            }
        }
        private static GraphQLHttpClient graphQLClient;
        public static GraphQLHttpClient GraphQLClient
        {
            get
            {
                if (graphQLClient == null)
                {
                    graphQLClient = new GraphQLHttpClient("http://localhost:4003/", new NewtonsoftJsonSerializer());

                    if (Dav.AccessToken != null)
                        graphQLClient.HttpClient.DefaultRequestHeaders.Add("Authorization", Dav.AccessToken);
                }

                return graphQLClient;
            }
        }

        public static async Task<UserResponse> RetrieveUser(int id)
        {
            var retrieveUserRequest = new GraphQLRequest
            {
                OperationName = "RetrieveUser",
                Query = @"
                    query RetrieveUser($id: Int!) {
                        retrieveUser(id: $id) {
                            firstName
                            profileImage
                        }
                    }
                ",
                Variables = new { id }
            };

            return (await GraphQLClient.SendQueryAsync<RetrieveUserResponse>(retrieveUserRequest)).Data.RetrieveUser;
        }

        public static async Task<bool> UploadSoundFile(string uuid, StorageFile file, string contentType)
        {
            HttpResponseMessage response;
            byte[] data = null;

            using (FileStream fs = File.OpenRead(file.Path))
            {
                var binaryReader = new BinaryReader(fs);
                data = binaryReader.ReadBytes((int)fs.Length);
            }

            var content = new ByteArrayContent(data);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            try
            {
                response = await HttpClient.PutAsync($"http://localhost:4003/sounds/{uuid}", content);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public static async Task<SoundResponse> RetrieveSound(string uuid)
        {
            var retrieveSoundRequest = new GraphQLRequest
            {
                OperationName = "RetrieveSound",
                Query = @"
                    query RetrieveSound($uuid: String!) {
                        retrieveSound(uuid: $uuid) {
                            name
                            description
                            audioFileUrl
                            type
                            source
                            user {
                                id
                                firstName
                                profileImage
                            }
                        }
                    }
                ",
                Variables = new { uuid }
            };

            return (await GraphQLClient.SendQueryAsync<RetrieveSoundResponse>(retrieveSoundRequest)).Data.RetrieveSound;
        }

        public static async Task<ListResponse<SoundResponse>> ListSounds(
            bool mine = false,
            int userId = 0,
            bool random = false,
            string query = null,
            int limit = 10,
            int offset = 0
        )
        {
            var listSoundsRequest = new GraphQLRequest
            {
                OperationName = "ListSounds",
                Query = @"
                    query ListSounds(
                        $mine: Boolean
                        $userId: Int
                        $random: Boolean
                        $query: String
                        $limit: Int
                        $offset: Int
                    ) {
                        listSounds(
                            mine: $mine
                            userId: $userId
                            random: $random
                            query: $query
                            limit: $limit
                            offset: $offset
                        ) {
                            total
                            items {
                                uuid
                                name
                                audioFileUrl
                            }
                        }
                    }
                ",
                Variables = new { mine, userId, random, query, limit, offset }
            };

            return (await GraphQLClient.SendQueryAsync<ListSoundsResponse>(listSoundsRequest)).Data.ListSounds;
        }

        public static async Task<SoundResponse> CreateSound(string name, string description = null)
        {
            var createSoundMutation = new GraphQLRequest
            {
                OperationName = "CreateSound",
                Query = @"
                    mutation CreateSound($name: String!, $description: String) {
                        createSound(name: $name, description: $description) {
                            uuid
                        }
                    }
                ",
                Variables = new { name, description }
            };

            return (await GraphQLClient.SendMutationAsync<CreateSoundResponse>(createSoundMutation)).Data.CreateSound;
        }
    }
}

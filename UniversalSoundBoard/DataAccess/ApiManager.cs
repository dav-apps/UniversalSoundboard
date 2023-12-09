using davClassLibrary;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using System;
using System.Collections.Generic;
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
                            uuid
                            name
                            description
                            audioFileUrl
                            type
                            source
                            tags
                            user {
                                id
                                firstName
                                profileImage
                            }
                            promotion {
                                uuid
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
            bool latest = false,
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
                        $latest: Boolean
                        $query: String
                        $limit: Int
                        $offset: Int
                    ) {
                        listSounds(
                            mine: $mine
                            userId: $userId
                            random: $random
                            latest: $latest
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
                Variables = new { mine, userId, random, latest, query, limit, offset }
            };

            return (await GraphQLClient.SendQueryAsync<ListSoundsResponse>(listSoundsRequest)).Data.ListSounds;
        }

        public static async Task<SoundResponse> CreateSound(string name, string description = null, List<string> tags = null)
        {
            var createSoundMutation = new GraphQLRequest
            {
                OperationName = "CreateSound",
                Query = @"
                    mutation CreateSound(
                        $name: String!
                        $description: String
                        $tags: [String!]
                    ) {
                        createSound(
                            name: $name
                            description: $description
                            tags: $tags
                        ) {
                            uuid
                        }
                    }
                ",
                Variables = new { name, description, tags }
            };

            return (await GraphQLClient.SendMutationAsync<CreateSoundResponse>(createSoundMutation)).Data.CreateSound;
        }

        public static async Task<SoundResponse> UpdateSound(string uuid, string name = null, string description = null, List<string> tags = null)
        {
            var updateSoundMutation = new GraphQLRequest
            {
                OperationName = "UpdateSound",
                Query = @"
                    mutation UpdateSound(
                        $uuid: String!
                        $name: String
                        $description: String
                        $tags: [String!]
                    ) {
                        updateSound(
                            uuid: $uuid
                            name: $name
                            description: $description
                            tags: $tags
                        ) {
                            uuid
                        }
                    }
                ",
                Variables = new { uuid, name, description, tags }
            };

            return (await GraphQLClient.SendMutationAsync<UpdateSoundResponse>(updateSoundMutation)).Data.UpdateSound;
        }

        public static async Task<SoundResponse> DeleteSound(string uuid)
        {
            var deleteSoundMutation = new GraphQLRequest
            {
                OperationName = "DeleteSound",
                Query = @"
                    mutation DeleteSound($uuid: String!) {
                        deleteSound(uuid: $uuid) {
                            uuid
                        }
                    }
                ",
                Variables = new { uuid }
            };

            return (await GraphQLClient.SendMutationAsync<DeleteSoundResponse>(deleteSoundMutation)).Data.DeleteSound;
        }

        public static async Task<SoundPromotionResponse> CreateSoundPromotion(string uuid)
        {
            var createSoundPromotionMutation = new GraphQLRequest
            {
                OperationName = "CreateSoundPromotion",
                Query = @"
                    mutation CreateSoundPromotion($uuid: String!) {
                        createSoundPromotion(uuid: $uuid) {
                            sessionUrl
                        }
                    }
                ",
                Variables = new { uuid }
            };

            return (await GraphQLClient.SendMutationAsync<CreateSoundPromotionResponse>(createSoundPromotionMutation)).Data.CreateSoundPromotion;
        }

        public static async Task<ListResponse<TagResponse>> ListTags(int limit = 10, int offset = 0)
        {
            var listTagsRequest = new GraphQLRequest
            {
                OperationName = "ListTags",
                Query = @"
                    query ListTags($limit: Int, $offset: Int) {
                        listTags(limit: $limit, offset: $offset) {
                            total
                            items {
                                name
                            }
                        }
                    }
                ",
                Variables = new { limit, offset }
            };

            return (await GraphQLClient.SendQueryAsync<ListTagsResponse>(listTagsRequest)).Data.ListTags;
        }
    }
}

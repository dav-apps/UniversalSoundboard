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

        #region Caching variables
        private static Dictionary<int, UserResponse> retrieveUserCache;
        private static Dictionary<int, UserResponse> RetrieveUserCache
        {
            get
            {
                if (retrieveUserCache == null)
                    retrieveUserCache = new Dictionary<int, UserResponse>();

                return retrieveUserCache;
            }
        }

        private static Dictionary<string, SoundResponse> retrieveSoundCache;
        private static Dictionary<string, SoundResponse> RetrieveSoundCache
        {
            get
            {
                if (retrieveSoundCache == null)
                    retrieveSoundCache = new Dictionary<string, SoundResponse>();

                return retrieveSoundCache;
            }
        }

        private static Dictionary<string, ListResponse<SoundResponse>> listSoundsCache;
        private static Dictionary<string, ListResponse<SoundResponse>> ListSoundsCache
        {
            get
            {
                if (listSoundsCache == null)
                    listSoundsCache = new Dictionary<string, ListResponse<SoundResponse>>();

                return listSoundsCache;
            }
        }

        private static Dictionary<string, ListResponse<TagResponse>> listTagsCache;
        private static Dictionary<string, ListResponse<TagResponse>> ListTagsCache
        {
            get
            {
                if (listTagsCache == null)
                    listTagsCache = new Dictionary<string, ListResponse<TagResponse>>();

                return listTagsCache;
            }
        }
        #endregion

        #region Caching methods
        public static void ClearListSoundsCache()
        {
            ListSoundsCache.Clear();
        }
        #endregion

        public static async Task<UserResponse> RetrieveUser(int id)
        {
            if (RetrieveUserCache.ContainsKey(id))
                return RetrieveUserCache.GetValueOrDefault(id);

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

            var response = await GraphQLClient.SendQueryAsync<RetrieveUserResponse>(retrieveUserRequest);
            var responseData = response?.Data?.RetrieveUser;

            if (responseData != null)
                RetrieveUserCache[id] = responseData;

            return responseData;
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

            return response.IsSuccessStatusCode;
        }

        public static async Task<SoundResponse> RetrieveSound(string uuid, bool caching = true)
        {
            if (RetrieveSoundCache.ContainsKey(uuid) && caching)
                return RetrieveSoundCache.GetValueOrDefault(uuid);

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

            var response = await GraphQLClient.SendQueryAsync<RetrieveSoundResponse>(retrieveSoundRequest);
            var responseData = response?.Data?.RetrieveSound;

            if (responseData != null)
                RetrieveSoundCache[uuid] = responseData;

            return responseData;
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
            string cacheKey = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}", mine, userId, random, latest, query, limit, offset);

            if (ListSoundsCache.ContainsKey(cacheKey))
                return ListSoundsCache.GetValueOrDefault(cacheKey);

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
                                duration
                            }
                        }
                    }
                ",
                Variables = new { mine, userId, random, latest, query, limit, offset }
            };

            var response = await GraphQLClient.SendQueryAsync<ListSoundsResponse>(listSoundsRequest);
            var responseData = response?.Data?.ListSounds;

            if (responseData != null)
                ListSoundsCache[cacheKey] = responseData;

            return responseData;
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

            var response = await GraphQLClient.SendMutationAsync<CreateSoundResponse>(createSoundMutation);
            return response?.Data?.CreateSound;
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

            var response = await GraphQLClient.SendMutationAsync<UpdateSoundResponse>(updateSoundMutation);
            return response?.Data?.UpdateSound;
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

            var response = await GraphQLClient.SendMutationAsync<DeleteSoundResponse>(deleteSoundMutation);
            return response?.Data?.DeleteSound;
        }

        public static async Task<SoundPromotionResponse> CreateSoundPromotion(string uuid, string title = null)
        {
            var createSoundPromotionMutation = new GraphQLRequest
            {
                OperationName = "CreateSoundPromotion",
                Query = @"
                    mutation CreateSoundPromotion($uuid: String!, $title: String) {
                        createSoundPromotion(uuid: $uuid, title: $title) {
                            sessionUrl
                        }
                    }
                ",
                Variables = new { uuid, title }
            };

            var response = await GraphQLClient.SendMutationAsync<CreateSoundPromotionResponse>(createSoundPromotionMutation);
            return response?.Data?.CreateSoundPromotion;
        }

        public static async Task<ListResponse<TagResponse>> ListTags(int limit = 10, int offset = 0)
        {
            string cacheKey = string.Format("{0}:{1}", limit, offset);

            if (ListTagsCache.ContainsKey(cacheKey))
                return ListTagsCache.GetValueOrDefault(cacheKey);

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

            var response = await GraphQLClient.SendQueryAsync<ListTagsResponse>(listTagsRequest);
            var responseData = response?.Data?.ListTags;

            if (responseData != null)
                ListTagsCache[cacheKey] = responseData;

            return responseData;
        }
    }
}

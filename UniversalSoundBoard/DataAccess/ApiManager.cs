using davClassLibrary;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using System.Threading.Tasks;
using UniversalSoundboard.Models;

namespace UniversalSoundboard.DataAccess
{
    public class ApiManager
    {
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

        public static async Task<ListResponse<SoundResponse>> ListSounds(
            bool mine = false,
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
                        $random: Boolean
                        $query: String
                        $limit: Int
                        $offset: Int
                    ) {
                        listSounds(
                            mine: $mine
                            random: $random
                            query: $query
                            limit: $limit
                            offset: $offset
                        ) {
                            total
                            items {
                                name
                                description
                                audioFileUrl
                                type
                                source
                            }
                        }
                    }
                ",
                Variables = new { mine, random, query, limit, offset }
            };

            return (await GraphQLClient.SendQueryAsync<ListSoundsResponse>(listSoundsRequest)).Data.ListSounds;
        }
    }
}

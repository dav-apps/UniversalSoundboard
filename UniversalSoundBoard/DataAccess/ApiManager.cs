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
                    graphQLClient = new GraphQLHttpClient("http://localhost:4003/", new NewtonsoftJsonSerializer());

                return graphQLClient;
            }
        }

        public static async Task<ListResponse<SoundResponse>> ListSounds(
            string query = null,
            bool random = false,
            int limit = 10
        )
        {
            var listSoundsRequest = new GraphQLRequest
            {
                OperationName = "ListSounds",
                Query = @"
                    query ListSounds($query: String, $random: Boolean, $limit: Int) {
                        listSounds(query: $query, random: $random, limit: $limit) {
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
                Variables = new { query, random, limit }
            };

            return (await GraphQLClient.SendQueryAsync<ListSoundsResponse>(listSoundsRequest)).Data.ListSounds;
        }
    }
}

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

        public static async Task<ListResponse<SoundResponse>> ListSounds()
        {
            var listSoundsRequest = new GraphQLRequest
            {
                Query = @"
                    query ListSounds($query: String) {
                        listSounds(query: $query) {
                            total
                            items {
                                name
                                description
                                audioFileUrl
                            }
                        }
                    }
                ",
                OperationName = "ListSounds"
            };

            return (await GraphQLClient.SendQueryAsync<ListSoundsResponse>(listSoundsRequest)).Data.ListSounds;
        }
    }
}

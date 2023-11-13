using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

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
                    graphQLClient = new GraphQLHttpClient("http://localhost:4002/", new NewtonsoftJsonSerializer());

                return graphQLClient;
            }
        }

        public static async Task ListSounds()
        {
            var listSoundsRequest = new GraphQLRequest
            {
                Query = @"
                    query ListSounds($query: String!) {
                        listSounds(query: $query) {
                            total
                            items {
                                name
                                description
                            }
                        }
                    }
                ",
                OperationName = "ListSounds",
                Variables = new {
                    query = "cars"
                }
            };

            var graphQLResponse = await GraphQLClient.SendQueryAsync<ListSoundsResponse>(listSoundsRequest);
            Debug.WriteLine(graphQLResponse.Data.ListSounds.Total);
            Debug.WriteLine(graphQLResponse.Data.ListSounds.Items[0].Name);
        }
    }

    public class ListSoundsResponse
    {
        public ListResponse<SoundResponse> ListSounds { get; set; }
    }

    public class SoundResponse
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class ListResponse<T>
    {
        public int Total { get; set; }
        public List<T> Items { get; set; }
    }
}

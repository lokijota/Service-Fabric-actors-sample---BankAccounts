using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestClientConsoleAzureSFCluster
{
    class Program
    {
        static HttpClient client = new HttpClient();

        static void Main()
        {
            RunAsync().Wait();
        }

        static async Task RunAsync()
        {
            client.BaseAddress = new Uri("http://sfjota.westeurope.cloudapp.azure.com:5001/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            Console.WriteLine("Creating actors");
            int n = 1000;
            List<string> accountsCreated = await CreateAccounts(n);
            Console.WriteLine("Created {0} actors", n);

            List<string> accountsGet = await GetAccountIds();
            Console.WriteLine("Got Ids of {0} actors", accountsGet.Count);

            int deleted = await DeleteAllTheActors();
            Console.WriteLine("Deleted {0} actors", deleted);

            accountsGet = await GetAccountIds();
            Console.WriteLine("Existing actors: {0}", accountsGet.Count);

            Console.ReadLine();
        }

        static async Task<List<string>> CreateAccounts(int count)
        {
            HttpResponseMessage response = await client.PostAsJsonAsync("api/accounts/create", count);
            response.EnsureSuccessStatusCode();
            List<string> accounts = await response.Content.ReadAsAsync<List<string>>();
            return accounts;
        }

        static async Task<List<string>> GetAccountIds()
        {
            HttpResponseMessage response = await client.GetAsync("api/accounts/get");
            response.EnsureSuccessStatusCode();
            List<string> accounts = await response.Content.ReadAsAsync<List<string>>();
            return accounts;
        }

        static async Task<int> DeleteAllTheActors()
        {
            HttpResponseMessage response = await client.GetAsync("api/accounts/deleteall");
            response.EnsureSuccessStatusCode();
            int count  = await response.Content.ReadAsAsync<int>();
            return count;
        }
    }
}

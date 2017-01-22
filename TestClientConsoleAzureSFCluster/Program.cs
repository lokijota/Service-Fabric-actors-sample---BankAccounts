using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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

            List<string> accounts = await CreateAccounts(1);

            Console.ReadLine();
        }

        static async Task<List<string>> CreateAccounts(int count)
        {
            HttpResponseMessage response = await client.PostAsJsonAsync("api/accounts", count);
            response.EnsureSuccessStatusCode();
            List<string> accounts = await response.Content.ReadAsAsync<List<string>>();

            // Return the URI of the created resource.
            return accounts;
        }

        //static void Main(string[] args)
        //{
        //}
    }
}

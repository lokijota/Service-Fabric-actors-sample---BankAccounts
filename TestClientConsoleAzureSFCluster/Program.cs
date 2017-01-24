namespace TestClientConsoleAzureSFCluster
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using SFActors.WebAPI.Contracts;

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

            // create actors
            Console.WriteLine("Creating actors...");
            int n = 20;
            Stopwatch watch = new Stopwatch(); watch.Start();
            List<string> accountsCreated = await CreateAccounts(n);
            watch.Stop();
            accountsCreated.Sort();
            Console.WriteLine("Created {0} actors in {1}ms", n, watch.ElapsedMilliseconds);

            // list actors and their partitions
            List<PartitionActors> partitionActors = await GetActorsInPartitions();
            List<string> accountsGet = partitionActors.SelectMany(pa => pa.ActorsInPartition).ToList();
            Console.WriteLine("Got Ids of {0} actors in {1} partitions", accountsGet.Count, partitionActors.Count());

            // get details for each actor
            double totalBalance = 0;
            foreach (string accountId in accountsCreated)
            {
                AccountDetail detail = await GetAccountBalance(accountId);
                totalBalance += detail.Balance;
                Console.WriteLine("Account {0} has balance {1,12:c}", detail.AccountNumber, detail.Balance);
            }
            Console.WriteLine("Total balance is {0,12:c}", totalBalance);

            Console.WriteLine(Environment.NewLine + "Press <Any> key to Create Standing orders...");
            Console.ReadKey(true);

            List<StandingOrder> standingOrders = CreateRandomListOfStandingOrders(accountsCreated, accountsCreated.Count*5);
            int count = await CreateStandingOrders(standingOrders);

            // show effects of standing orders
            do
            {
                totalBalance = 0;

                Console.Clear();
                foreach (string accountId in accountsCreated)
                {
                    AccountDetail detail = await GetAccountBalance(accountId);
                    totalBalance += detail.Balance;
                    if (detail.Balance % 10 != 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    }
                    Console.WriteLine("Account {0} has balance {1,12:c}", detail.AccountNumber, detail.Balance);
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                Console.WriteLine("## Total balance is {0:c}", totalBalance);
                Thread.Sleep(5000);
            } while (Console.KeyAvailable == false);
            Console.ReadKey(true);

            Console.Write("Creating loads more actors/standing orders until a key is pressed");
            do
            {
                List<string> newAccounts = await CreateAccounts(n);
                standingOrders = CreateRandomListOfStandingOrders(newAccounts, n);
                Console.Write(".");
            } while (Console.KeyAvailable == false);
            Console.ReadKey(true);


            Console.Write(Environment.NewLine + "Deleting actors... ");

            int deleted = await DeleteAllTheActors();
            Console.WriteLine("deleted {0} actors", deleted);

            partitionActors = await GetActorsInPartitions();
            Console.WriteLine("Existing actors: {0}", partitionActors.SelectMany(pa => pa.ActorsInPartition).Count());

            Console.ReadLine();
        }

        #region auxiliary methods - prepare data

        private static List<StandingOrder> CreateRandomListOfStandingOrders(List<string> accounts, int count)
        {
            List<StandingOrder> standingOrders = new List<StandingOrder>();
            Random r = new Random();

            for (int j = 0; j < count-2; j++)
            {
                int posSource = r.Next(0, accounts.Count);
                int posTarget;

                // pick a target account
                do
                {
                    posTarget = r.Next(0, accounts.Count);
                } while (posSource == posTarget);

                StandingOrder newSO = new StandingOrder()
                {
                    FromAccount = accounts[posSource],
                    ToAccount = accounts[posTarget],
                    Amount = Math.Round(r.NextDouble() * 200, 2),
                    RecurrenceMinute = j < 2? (short) (DateTime.Now.Minute + 1) : (short) r.Next(0, 60)
                };

                standingOrders.Add(newSO);
            }

            return standingOrders;
        }

        #endregion

        #region methods to call call webapi

        static async Task<List<string>> CreateAccounts(int count)
        {
            HttpResponseMessage response = await client.PostAsJsonAsync("api/accounts/create", count);
            response.EnsureSuccessStatusCode();
            List<string> accounts = await response.Content.ReadAsAsync<List<string>>();
            return accounts;
        }

        static async Task<List<PartitionActors>> GetActorsInPartitions()
        {
            HttpResponseMessage response = await client.GetAsync("api/accounts/get");
            response.EnsureSuccessStatusCode();
            List<PartitionActors> accounts = await response.Content.ReadAsAsync<List<PartitionActors>>();
            return accounts;
        }

        static async Task<int> CreateStandingOrders(List<StandingOrder> standingOrders)
        {
            HttpResponseMessage response = await client.PostAsJsonAsync("api/accounts/createstandingorders", standingOrders);
            response.EnsureSuccessStatusCode();
            int count = await response.Content.ReadAsAsync<int>();
            return count;
        }

        static async Task<AccountDetail> GetAccountBalance(string account)
        {
            //http://sfjota.westeurope.cloudapp.azure.com:5001/api/accounts/getbalance?accountId=18333895
            HttpResponseMessage response = await client.GetAsync(string.Format("api/accounts/getbalance?accountId={0}", account));
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsAsync<AccountDetail>();
        }

        static async Task<int> DeleteAllTheActors()
        {
            HttpResponseMessage response = await client.GetAsync("api/accounts/deleteall");
            response.EnsureSuccessStatusCode();
            int count  = await response.Content.ReadAsAsync<int>();
            return count;
        }

        #endregion
    }
}

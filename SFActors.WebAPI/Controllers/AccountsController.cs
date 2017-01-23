using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using AutoPoco;
using AutoPoco.DataSources;
using AutoPoco.Engine;
using BankAccount.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Query;

namespace SFActors.WebAPI.Controllers
{
    [ServiceRequestActionFilter]
    public class AccountsController : ApiController
    {
        // GET api/accounts/get 
        [HttpGet]
        [ActionName("get")]
        public IEnumerable<string> Get()
        {
            ServiceEventSource.Current.Message("@AccountsController.Get");

            Int64 highKey = 9223372036854775807;
            Int64 lowKey = -9223372036854775808;
            var partitionCount = 10;
            var partitionRange = highKey / partitionCount - lowKey / partitionCount; //40

            CancellationToken cancellationToken = default(CancellationToken);

            List<ActorInformation> actorInformationList = new List<ActorInformation>();

            for (int i = 0; i < partitionCount; i++)
            {
                //this would generate keys as -100, -60, -20, 20, 60
                var randomPartitionKey = i * partitionRange + lowKey;

                // {your service fabric Uri}
                var actorProxy = ActorServiceProxy.Create(new Uri("fabric:/SFActors.BankAccounts/BankAccountActorService"), randomPartitionKey);
                
                ServiceEventSource.Current.Message("Get.3");

                ContinuationToken continuationToken = null;
                do
                {
                    PagedResult<ActorInformation> page = actorProxy.GetActorsAsync(continuationToken, cancellationToken).GetAwaiter().GetResult();
                    actorInformationList.AddRange(page.Items);
                    continuationToken = page.ContinuationToken;
                } while (continuationToken != null);
            }

            ServiceEventSource.Current.Message("@AccountsController.Count=" + actorInformationList.Count);

            IEnumerable<string> actorIds = actorInformationList.Select(x => x.ActorId.GetStringId());
            return actorIds;

            //return new string[] { "value1", "value2" };
        }

        // GET api/accounts/deleteall 
        [HttpGet]
        [ActionName("deleteall")] 
        public int DeleteAll(int bogus = 1)
        {
            IEnumerable<string> actorIds = Get();
            CancellationToken cancelationToken = default(CancellationToken);
            int count = 0;


            foreach (string actorId in actorIds)
            {
                ActorId actorToDelete = new ActorId(actorId);
                IActorService myActorServiceProxy = ActorServiceProxy.Create(new Uri("fabric:/SFActors.BankAccounts/BankAccountActorService"), actorToDelete);
                myActorServiceProxy.DeleteActorAsync(actorToDelete, cancelationToken);
                count++;
            }

            return count;
        }


        //// GET api/values/5 
        //public string Get(int id)
        //{
        //    return "value";
        //}

        // POST api/values 
        [HttpPost]
        [ActionName("somepost")] //Route
        public void Post([FromBody]int count)
        {
            ServiceEventSource.Current.Message("Entrei Post");
        }

        [HttpPost]
        [ActionName("create")]
        public List<string> CreateAccounts([FromBody] int count)
        {
            List<string> _accounts = new List<string>();
            Random r = new Random();

            #region Random customer name generator with AutoPoco
            // Perform factory set up (once for entire test run)
            IGenerationSessionFactory factory = AutoPocoContainer.Configure(x =>
            {
                x.Conventions(c =>
                {
                    c.UseDefaultConventions();
                });
                x.AddFromAssemblyContainingType<Person>();
                x.Include<Person>().Setup(p => p.FirstName).Use<FirstNameSource>()
                                   .Setup(p => p.LastName).Use<LastNameSource>();
            });

            // Generate one of these per test (factory will be a static variable most likely)
            IGenerationSession session = factory.CreateSession();

            #endregion

            #region Create N bank accounts

            ServiceEventSource.Current.Message("@AccountsController.Create - Instanciating and initializating '{0}' actors", count);
            // Console.WriteLine(": ");

            for (int j = 0; j < count; j++)
            {
                // generate random account number
                string accountNumber = r.Next(0, 50000000).ToString("00000000");

                // generate name of customer
                Person p = session.Single<Person>().Get();
                string accountOwner = p.FirstName + " " + p.LastName;

                // generate starting balance for the account
                int startingBalance = r.Next(0, 10000);

                // 'create' and initialize the actor
                ActorId newActorId = new ActorId(accountNumber);

                IBankAccount newBankAccount = ActorProxy.Create<IBankAccount>(newActorId, "fabric:/SFActors.BankAccounts");
                newBankAccount.InitializeState(accountOwner, startingBalance).GetAwaiter().GetResult();

                BankAccountStateBase state = newBankAccount.GetAccountInfo().GetAwaiter().GetResult();

                ServiceEventSource.Current.Message("@AccountsController.Create - " + state.CustomerName + " has £" + state.Balance + " in account nb: " + state.AccountNumber);

                _accounts.Add(accountNumber);
            }

            #endregion

            return _accounts;
        }


        #region Create 100 Standing Orders

        [HttpPost]
        [ActionName("createstandingorders")]
        public void CreateStandingOrders([FromBody] List<string> accounts) // int how many to create -> data contract
        {
            Random r = new Random();

            for (int j = 0; j < 100; j++)
            {
                int posSource = r.Next(0, accounts.Count);
                int posTarget = r.Next(0, accounts.Count);

                if (posSource == posTarget)
                {
                    // one less transfer...
                    continue;
                }

                ActorId sourceAccountId = new ActorId(accounts[posSource]);
                IBankAccount sourceAccountProxy = ActorProxy.Create<IBankAccount>(sourceAccountId, "fabric:/SFActors.BankAccounts");

                double howMuch = r.NextDouble() * 500;
                short onMinute = (short) r.Next(0, 60);
                sourceAccountProxy.AddStandingOrder(accounts[posTarget], howMuch, onMinute);

                ServiceEventSource.Current.Message("SO payable to account {0} of £{1:f2} on minute {2}", accounts[posTarget], howMuch, onMinute);
            }

            #endregion
        }


            //// PUT api/values/5 
            //public void Put(int id, [FromBody]string value)
            //{
            //}

            //// DELETE api/values/5 
            //public void Delete(int id)
            //{
            //}
        }

/// <summary>
/// Internal class used only to generate names with AutoPoco
/// </summary>
public class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}

namespace SFActors.WebAPI.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Fabric; // for the partition resolver
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;
    using AutoPoco;
    using AutoPoco.DataSources;
    using AutoPoco.Engine;
    using BankAccount.Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;  // for the partition resolver
    using Microsoft.ServiceFabric.Actors.Query;
    using Microsoft.ServiceFabric.Services.Client;
    using SFActors.WebAPI.Contracts;

    [ServiceRequestActionFilter]
    public class AccountsController : ApiController
    {
        // GET api/accounts/get 
        [HttpGet]
        [ActionName("get")]
        public List<PartitionActors> Get()
        {
            Int64 highKey = 9223372036854775807;
            Int64 lowKey = -9223372036854775808;
            int partitionCount = 10;
            Int64 partitionRange = highKey/partitionCount - lowKey/partitionCount; // number of elements per interval of range
            int actorCount = 0;

            CancellationToken cancellationToken = default(CancellationToken);

            List<PartitionActors> partitionActors = new List<PartitionActors>();
            List<ActorInformation> actorInformationList = new List<ActorInformation>();

            for (int i = 0; i < partitionCount; i++)
            {
                // this generates a key in each of the partitions
                var partitionKeyInPartition = lowKey + i * partitionRange + 10;

                // note proxy to actor service, not a specific actor
                var actorProxy = ActorServiceProxy.Create(new Uri("fabric:/SFActors.BankAccounts/BankAccountActorService"), partitionKeyInPartition);

                // get all the actors in the partition
                ContinuationToken continuationToken = null;
                do
                {
                    PagedResult<ActorInformation> page = actorProxy.GetActorsAsync(continuationToken, cancellationToken).GetAwaiter().GetResult();
                    actorInformationList.AddRange(page.Items);
                    continuationToken = page.ContinuationToken;
                } while (continuationToken != null);

                // find the partition id for the current partition key
                ServicePartitionKey partitionKey = new ServicePartitionKey(partitionKeyInPartition);
                ServicePartitionResolver servicePartitionResolver = ServicePartitionResolver.GetDefault();
                ResolvedServicePartition partition = servicePartitionResolver.ResolveAsync(new Uri("fabric:/SFActors.BankAccounts/BankAccountActorService"), partitionKey, cancellationToken).GetAwaiter().GetResult();

                // prepare the result
                if (actorInformationList.Count > 0)
                {
                    PartitionActors pa = new PartitionActors
                    {
                        PartitionId = partition.Info.Id,
                        ActorsInPartition = actorInformationList.Select(x => x.ActorId.GetStringId()).ToList()
                    };
                    partitionActors.Add(pa);

                    actorCount += actorInformationList.Count;
                    actorInformationList.Clear();
                }
            }

            ServiceEventSource.Current.Message("@AccountsController. {0} actors in {1} partitions", actorCount, partitionActors.Count);

            return partitionActors;
        }

        [HttpGet]
        [ActionName("getbalance")]
        public AccountDetail GetAccountBalance(string accountId)
        {
            ServiceEventSource.Current.Message("@AccountsController.GetAccountBalance");

            ActorId actorId = new ActorId(accountId);

            IBankAccount bankAccount = ActorProxy.Create<IBankAccount>(actorId, "fabric:/SFActors.BankAccounts");
            BankAccountStateBase state = bankAccount.GetAccountInfo().GetAwaiter().GetResult();

            return new AccountDetail { AccountNumber = state.AccountNumber, Balance = state.Balance };
        }

        // GET api/accounts/deleteall 
        [HttpGet]
        [ActionName("deleteall")]
        public int DeleteAll(int bogus = 1)
        {
            IEnumerable<string> actorIds = Get().SelectMany(x => x.ActorsInPartition);
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


        [HttpPost]
        [ActionName("create")]
        public List<string> CreateAccounts([FromBody] int count)
        {
            List<string> accounts = new List<string>();
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

                // generate starting balance for the account. Always multiple of 10 to make it easier to detect changes because of transfers
                int startingBalance = r.Next(0, 10000) * 10;

                // 'create' and initialize the actor
                ActorId newActorId = new ActorId(accountNumber);
                IBankAccount newBankAccount = ActorProxy.Create<IBankAccount>(newActorId, "fabric:/SFActors.BankAccounts");
                newBankAccount.InitializeState(accountOwner, startingBalance).GetAwaiter().GetResult();

                // debug
                BankAccountStateBase state = newBankAccount.GetAccountInfo().GetAwaiter().GetResult();
                ServiceEventSource.Current.Message("@AccountsController.Create - " + state.CustomerName + " has €" + state.Balance + " in account nb: " + state.AccountNumber);

                accounts.Add(accountNumber);
            }

            #endregion

            return accounts;
        }


        [HttpPost]
        [ActionName("createstandingorders")]
        public int CreateStandingOrders([FromBody] List<Contracts.StandingOrder> standingOrders)
        {
            int count = 0;

            foreach (Contracts.StandingOrder so in standingOrders)
            {
                ActorId sourceAccountId = new ActorId(so.FromAccount);
                IBankAccount sourceAccountProxy = ActorProxy.Create<IBankAccount>(sourceAccountId, "fabric:/SFActors.BankAccounts");
                sourceAccountProxy.AddStandingOrder(so.ToAccount, so.Amount, so.RecurrenceMinute);
                count++;

                ServiceEventSource.Current.Message("SO payable to account {0} of €{1:f2} on minute {2}", so.ToAccount, so.Amount, so.RecurrenceMinute);
            }

            return count;
        }
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

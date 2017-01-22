using System;
using System.Collections.Generic;
using System.Web.Http;
using AutoPoco;
using AutoPoco.DataSources;
using AutoPoco.Engine;
using BankAccount.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;

namespace SFActors.WebAPI.Controllers
{
    [ServiceRequestActionFilter]
    public class AccountsController : ApiController
    {
        // GET api/values 
        public IEnumerable<string> Get()
        {
            ServiceEventSource.Current.Message("Entrei no Get");
            return new string[] { "value1", "value2" };
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

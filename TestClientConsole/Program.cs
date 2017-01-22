namespace TestClientConsole
{
    using System;
    using System.Collections.Generic;
    using AutoPoco;
    using AutoPoco.DataSources;
    using AutoPoco.Engine;
    using BankAccount.Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;

    class Program
    {
        static void Main(string[] args)
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

            #region Create 100 bank accounts

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Instanciating and initializating 100 actors: ");
            Console.ForegroundColor = ConsoleColor.Gray;

            for (int j = 0; j < 100; j++)
            {
                // generate account number
                string accountNumber = r.Next(0, 50000000).ToString("00000000");

                // generate name
                Person p = session.Single<Person>().Get();
                string accountOwner = p.FirstName + " " + p.LastName;

                // generate starting balance
                int startingBalance = r.Next(0, 10000);

                // 'create' the actor
                ActorId newActorId = new ActorId(accountNumber);


                IBankAccount newBankAccount = ActorProxy.Create<IBankAccount>(newActorId, "fabric:/SFActors.BankAccounts");
                newBankAccount.InitializeState(accountOwner, startingBalance).GetAwaiter().GetResult();

                BankAccountStateBase state = newBankAccount.GetAccountInfo().GetAwaiter().GetResult();
                Console.WriteLine(state.CustomerName + " has £" + state.Balance + " in account nb: " + state.AccountNumber);

                _accounts.Add(accountNumber);
            }

            #endregion

            #region Create 100 Standing Orders

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("100 Bank Account actors created. Press a key to create 100 standing orders.");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.ReadLine();

            for (int j = 0; j < 100; j++)
            {
                int posSource = r.Next(0, _accounts.Count);
                int posTarget = r.Next(0, _accounts.Count);

                if (posSource == posTarget)
                {
                    // one less transfer...
                    continue;
                }

                ActorId sourceAccountId = new ActorId(_accounts[posSource]);
                IBankAccount sourceAccountProxy = ActorProxy.Create<IBankAccount>(sourceAccountId, "fabric:/SFActors.BankAccounts");

                double howMuch = r.NextDouble() * 500;
                short onMinute = (short) r.Next(0, 60);
                sourceAccountProxy.AddStandingOrder(_accounts[posTarget], howMuch, onMinute);
                Console.WriteLine("SO payable to account {0} of £{1:f2} on minute {2}", _accounts[posTarget], howMuch, onMinute);
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("100 Standing orders registered created");
            Console.ForegroundColor = ConsoleColor.Gray;

            #endregion

            #region GO CRAZY with creating objects

            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Enter how many more actors you want to create (and SO's):");
            string more = Console.ReadLine();
            Console.ForegroundColor = ConsoleColor.Gray;

            int howManyMore = int.Parse(more);

            for(int j=0; j<howManyMore; j++)
            {
                // generate account number
                string accountNumber = r.Next(0, 50000000).ToString("00000000");

                // generate name
                Person p = session.Single<Person>().Get();
                string accountOwner = p.FirstName + " " + p.LastName;

                // generate starting balance
                int startingBalance = r.Next(0, 10000);

                // 'create' the actor
                ActorId newActorId = new ActorId(accountNumber);
                IBankAccount newBankAccount = ActorProxy.Create<IBankAccount>(newActorId, "fabric:/SFActors.BankAccounts");
                newBankAccount.InitializeState(accountOwner, startingBalance).GetAwaiter().GetResult();
                Console.Write("A");

                _accounts.Add(accountNumber);
            }

            for (int j = 0; j < howManyMore; j++)
            {
                int posSource = r.Next(0, _accounts.Count);
                int posTarget = r.Next(0, _accounts.Count);

                if (posSource == posTarget)
                {
                    // one less transfer...
                    continue;
                }

                ActorId sourceAccountId = new ActorId(_accounts[posSource]);
                IBankAccount sourceAccountProxy = ActorProxy.Create<IBankAccount>(sourceAccountId, "fabric:/SFActors.BankAccounts");

                double howMuch = r.NextDouble() * 500;
                short onMinute = (short) r.Next(0, 60);
                sourceAccountProxy.AddStandingOrder(_accounts[posTarget], howMuch, onMinute);
                Console.Write("S");
            }

            #endregion

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Done! Press any key to exit this tool.");
            Console.ReadLine();
            Console.ForegroundColor = ConsoleColor.Gray;
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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AutoPoco;
using AutoPoco.DataSources;
using AutoPoco.Engine;
using BankAccount.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;

namespace TestClient
{
    public partial class Form1 : Form
    {
        private List<string> _accounts = new List<string>();

        public Form1()
        {
            InitializeComponent();
        }

        private void create100button_Click(object sender, EventArgs e)
        {
            #region Random customer name generator
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

            progressBar1.Value = 0;

            for (int j = 0; j < 20; j++)
            {
                // generate account number
                Random r = new Random();
                string accountNumber = r.Next(0, 50000000).ToString("00000000");

                // generate name
                Person p = session.Single<Person>().Get();
                string accountOwner = p.FirstName + " " + p.LastName;

                // generate starting balance
                int startingBalance = r.Next(0, 10000);

                // 'create' the actor
                ActorId newActorId = new ActorId(accountNumber);
                IBankAccount newBankAccount = ActorProxy.Create<IBankAccount>(newActorId, "fabric:/SFActors.BankAccounts");
                System.Threading.Thread.Sleep(200);

                newBankAccount.InitializeState(accountOwner, startingBalance).GetAwaiter().GetResult();
                System.Threading.Thread.Sleep(200);

                //BankAccountStateBase state = newBankAccount.GetAccountInfo().GetAwaiter().GetResult();
                //textBox1.Text += state.CustomerName + " has £" + state.Balance + " in account " + state.AccountNumber + Environment.NewLine;
                //System.Threading.Thread.Sleep(200);

                _accounts.Add(accountNumber);

                progressBar1.PerformStep();
            }

            textBox1.Text += "100 Bank Account actors created" + Environment.NewLine;
        }

        private void createSObutton_Click(object sender, EventArgs e)
        {
            progressBar1.Value = 0;
            Random r = new Random();

            for(int j=0; j<100; j++)
            {
                int posSource = r.Next(0, _accounts.Count);
                int posTarget = r.Next(0, _accounts.Count);

                if(posSource == posTarget)
                {
                    // one less transfer...
                    continue;
                }

                ActorId sourceAccountId = new ActorId(_accounts[posSource]);
                IBankAccount sourceAccountProxy = ActorProxy.Create<IBankAccount>(sourceAccountId, "fabric:/SFActors.BankAccounts");

                sourceAccountProxy.AddStandingOrder(_accounts[posTarget], r.NextDouble() * 500, (short) r.Next(0, 61));
                System.Threading.Thread.Sleep(100);

                progressBar1.PerformStep();
            }

            textBox1.Text += "100 Standing orders registered created" + Environment.NewLine;
        }
    }

    // internal class used only to generate names
    public class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
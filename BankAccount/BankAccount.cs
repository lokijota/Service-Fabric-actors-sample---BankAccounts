namespace BankAccount
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Microsoft.ServiceFabric.Data;

    /// <remarks>
    /// This class represents an actor.
    /// Every ActorID maps to an instance of this class.
    /// The StatePersistence attribute determines persistence and replication of actor state:
    ///  - Persisted: State is written to disk and replicated.
    ///  - Volatile: State is kept in memory only and replicated.
    ///  - None: State is kept in memory only and not replicated.
    /// </remarks>
    [StatePersistence(StatePersistence.Persisted)]
    internal class BankAccount : Actor, IBankAccount, IRemindable
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of BankAccount - not ActorId
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public BankAccount(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        #endregion

        /// <summary>
        /// This method is called whenever an actor is activated. 
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override async Task OnActivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, "@BankAccount.OnActivateAsync for account '{0}'", Id.GetStringId());

            // Add state if it doesn't exist. doesn't fail if state already exists
            StateManager.TryAddStateAsync("AccountState", null as BankAccountState).GetAwaiter().GetResult();

            // register a reminder 
            //Question: should this be in constructor? if Reminders are persisted, they shouldn't have to be created on Activation...?
            IActorReminder actorReminder = await this.RegisterReminderAsync(
                "process standing orders",     // reminder name
                null,                          // parameter to reminder - must be byte[]
                TimeSpan.FromSeconds(5),       // Amount of time to delay before the reminder is invoked
                TimeSpan.FromMinutes(1));      // Time interval between invocations of the reminder method

            return;
        }

        /// <summary>
        /// Initialize the state.
        /// TODO: isolate the State object in 3 differnt buckets: base info, operations, standing orders.
        /// This will improve perf in serialize/deserialize
        /// </summary>
        public Task InitializeState(string accountOwner, double openingBalance)
        {
            ActorEventSource.Current.ActorMessage(this, "@BankAccount.InitializeState for account '{0}'", Id.GetStringId());

            BankAccountState state = new BankAccountState();
            state.AccountNumber = Id.GetStringId(); // accountNumber;
            state.CustomerName = accountOwner;
            state.Balance = openingBalance;

            state.LastOperations = new List<Operation>();
            state.StandingOrders = new List<StandingOrder>();

            // inconditional set of data in instance's state
            return StateManager.SetStateAsync("AccountState", state);
        }

        /// <summary>
        ///  Get base information on the account
        /// </summary>
        public async Task<BankAccountStateBase> GetAccountInfo()
        {
            ActorEventSource.Current.ActorMessage(this, "@BankAccount.GetAccountInfo for account '{0}'", Id.GetStringId());

            BankAccountStateBase state = await StateManager.GetStateAsync<BankAccountStateBase>("AccountState");

            return state;
        }

        /// <summary>
        /// Receive a payment from another Actor or client
        /// </summary>
        public async Task<bool> Transfer(string sourceAccount, double amount, DateTime when, int uniqueOperationId)
        {
            ActorEventSource.Current.ActorMessage(this, "@BankAccount.Transfer for account '{0}' of {1}", Id.GetStringId(), amount);

            BankAccountState state = await StateManager.GetStateAsync<BankAccountState>("AccountState");

            // idempotency protection for repeated messages
            foreach(Operation op in state.LastOperations)
            {
                if(op.OperationId == uniqueOperationId)
                {
                    return false;
                }
            }

            // validate positive amount (we are receiving money)
            if(amount <= 0)
            {
                return false;
            }

            // accept the money :-)
            state.LastOperations.Add(new Operation
            {
                AccountNumber = sourceAccount,
                Amount = amount,
                When = when,
                OperationId = uniqueOperationId
            });

            state.Balance += amount;

            await StateManager.SetStateAsync("AccountState", state);

            return true;
        }

        /// <summary>
        /// Add a standing order to a given account
        /// </summary>
        public async Task AddStandingOrder(string toAccount, double amount, short minute)
        {
            ActorEventSource.Current.ActorMessage(this, "@BankAccount.AddStandingOrder for account '{0}', min = {1}", Id.GetStringId(), minute);

            BankAccountState state = await StateManager.GetStateAsync<BankAccountState>("AccountState");

            state.StandingOrders.Add(new StandingOrder
            {
                Amount = amount,
                ToAccountNumber = toAccount,
                RecurrenceMinute = minute
            });

            StateManager.SetStateAsync("AccountState", state).GetAwaiter().GetResult(); // could have just returned thread here -- testing

            return;
        }

        /// <summary>
        /// Actor's Reminder - 1 hearbeat per minute
        /// </summary>
        public async Task ReceiveReminderAsync(string reminderName, byte[] context, TimeSpan dueTime, TimeSpan period)
        {
            if (reminderName.Equals("process standing orders"))
            {
                BankAccountState state = StateManager.GetStateAsync<BankAccountState>("AccountState").GetAwaiter().GetResult();

                DateTime now = DateTime.Now;

                foreach (StandingOrder so in state.StandingOrders)
                {
                    if (so.RecurrenceMinute == now.Minute)
                    {
                        ActorEventSource.Current.ActorMessage(this, "@BankAccount.ReceiveReminderAsync paying from '{0}' to '{1}': €{2:f2}", Id.GetStringId(), so.ToAccountNumber, so.Amount);

                        // time to pay, says Roy Batty
                        Random r = new Random();
                        int newOperationId = r.Next(0, int.MaxValue);

                        // remove money from this account
                        state.Balance -= so.Amount;

                        state.LastOperations.Add(new Operation
                        {
                            AccountNumber = so.ToAccountNumber,
                            Amount = -so.Amount,
                            When = now,
                            OperationId = newOperationId
                        });

                        await StateManager.SetStateAsync("AccountState", state); // test if it can cause issues: mutiple payments in same account/minute?

                        // send money to target account
                        IBankAccount accountProxy = ActorProxy.Create<IBankAccount>(new ActorId(so.ToAccountNumber), "fabric:/SFActors.BankAccounts");
                        await accountProxy.Transfer(Id.GetStringId(), so.Amount, now, newOperationId);
                    }
                }
            }

            return;
        }
    }
}

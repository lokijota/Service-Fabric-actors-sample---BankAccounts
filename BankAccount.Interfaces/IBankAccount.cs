namespace BankAccount.Interfaces
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors;

    /// <summary>
    /// This interface defines the methods exposed by an actor.
    /// Clients use this interface to interact with the actor that implements it.
    /// </summary>
    public interface IBankAccount : IActor
    {
        Task InitializeState(string accountOwner, double openingBalance);

        Task<BankAccountStateBase> GetAccountInfo();

        Task<bool> Transfer(string sourceAccount, double amount, DateTime when, int uniqueOperationId);

        Task AddStandingOrder(string toAccount, double amount, short minute);
    }
}
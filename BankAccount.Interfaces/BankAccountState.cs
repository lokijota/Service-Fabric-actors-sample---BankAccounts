namespace BankAccount.Interfaces
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Detailed information in BankAccount's state
    /// NOTE: this should in different StateManager objects, no all in the same object... TODO.
    /// </summary>
    [DataContract]
    public class BankAccountState : BankAccountStateBase
    {
        [DataMember]
        public List<StandingOrder> StandingOrders { get; set; }

        [DataMember]
        public List<Operation> LastOperations { get; set; }
    }
}

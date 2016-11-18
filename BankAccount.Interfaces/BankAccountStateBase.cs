namespace BankAccount.Interfaces
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Base information about a bank account
    /// </summary>
    [DataContract]
    [KnownType(typeof(BankAccountState))]
    public class BankAccountStateBase
    {
        [DataMember]
        public string AccountNumber { get; set; }

        [DataMember]
        public string CustomerName { get; set; }

        [DataMember]
        public double Balance { get; set; }
    }
}

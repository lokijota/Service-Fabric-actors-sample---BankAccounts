namespace BankAccount.Interfaces
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    public class Operation
    {
        /// <summary>
        /// Source or target account
        /// </summary>
        [DataMember]
        public string AccountNumber { get; set; }

        [DataMember]
        public DateTime When { get; set; }

        /// <summary>
        /// Negative if payment made, Positive if payment received
        /// </summary>
        [DataMember]
        public double Amount { get; set; }

        [DataMember]
        public int OperationId { get; set; }
    }
}
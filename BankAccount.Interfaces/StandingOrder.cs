namespace BankAccount.Interfaces
{
    using System.Runtime.Serialization;

    [DataContract]
    public class StandingOrder
    {
        [DataMember]
        public string ToAccountNumber { get; set; }

        [DataMember]
        public double Amount { get; set; }

        /// <summary>
        /// So that an interesting demo works out, instead of saying what is the day the SO is paid, 
        /// the minute is used. So if you have a large number of Standing Orders with random minutes,
        /// it's very likely you'll see payments being made even in a short demo.
        /// </summary>
        [DataMember]
        public short RecurrenceMinute { get; set; }
    }
}
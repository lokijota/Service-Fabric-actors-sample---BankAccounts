namespace SFActors.WebAPI.Contracts
{
    public class StandingOrder
    {
        public string FromAccount { get; set; }
        public string ToAccount { get; set; }
        public double Amount { get; set; }

        /// <summary>
        /// So that an interesting demo works out, instead of saying what is the day the SO is paid, 
        /// the minute is used. So if you have a large number of Standing Orders with random minutes,
        /// it's very likely you'll see payments being made even in a short demo.
        /// </summary>
        public short RecurrenceMinute { get; set; }
    }
}
namespace SFActors.WebAPI.Contracts
{
    using System;
    using System.Collections.Generic;

    public class PartitionActors
    {
        public Guid PartitionId { get; set; }

        public List<string> ActorsInPartition { get; set; }
    }
}

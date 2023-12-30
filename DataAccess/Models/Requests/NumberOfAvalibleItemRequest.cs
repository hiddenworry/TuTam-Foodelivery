namespace DataAccess.Models.Requests
{
    public class NumberOfAvalibleItemRequest
    {
        public List<Guid> ItemIds { get; set; }

        public List<ScheduledTime> ScheduledTimes { get; set; }

        public Guid? BranchId { get; set; }
    }
}

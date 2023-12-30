namespace DataAccess.Models.Requests
{
    public class StockGettingRequest
    {
        public Guid ItemId { get; set; }

        public List<ScheduledTime> ScheduledTimes { get; set; }
    }
}

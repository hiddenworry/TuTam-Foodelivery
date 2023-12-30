namespace DataAccess.Models.Responses
{
    public class StatisticObjectByTimeRangeResponse
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public int Quantity { get; set; }
    }
}

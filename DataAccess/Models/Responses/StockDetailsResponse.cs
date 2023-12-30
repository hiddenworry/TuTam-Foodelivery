namespace DataAccess.Models.Responses
{
    public class StockDetailsResponse
    {
        public Guid ItemId { get; set; }

        public Guid StockId { get; set; }
        public double Quantity { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime ExprirationDate { get; set; }

        public string Status { get; set; }

        public string Unit { get; set; }
    }
}

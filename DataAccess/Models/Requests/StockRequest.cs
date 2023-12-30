namespace DataAccess.Models.Requests
{
    public class StockRequest
    {
        public Guid StockId { get; set; }

        public double Quantity { get; set; }

        public string? Note { get; set; }
    }
}

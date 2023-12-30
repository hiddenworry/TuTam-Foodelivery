namespace DataAccess.Models.Responses
{
    public class SimpleStockUpdatedHistoryDetailResponse
    {
        public Guid StockId { get; set; }

        public string? StockCode { get; set; }

        public double Quantity { get; set; }

        public DateTime ExpirationDate { get; set; }
    }
}

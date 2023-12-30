namespace DataAccess.Models.Responses
{
    public class DeliveryItemResponse
    {
        public Guid DeliveryItemId { get; set; }

        public string Name { get; set; }

        public string Image { get; set; }

        public string Unit { get; set; }

        public double Quantity { get; set; }

        public List<SimpleStockUpdatedHistoryDetailResponse>? Stocks { get; set; }

        public DateTime? InitialExpirationDate { get; set; }

        public double? ReceivedQuantity { get; set; }
    }
}

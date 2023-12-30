namespace DataAccess.Models.Responses
{
    public class StockUpdatedHistoryDetailForSelfShippingResponse
    {
        public Guid StockUpdatedHistoryDetailId { get; set; }

        public string Name { get; set; }

        public string Image { get; set; }

        public string Unit { get; set; }

        public DateTime ConfirmedExpirationDate { get; set; }

        public double ExportedQuantity { get; set; }
    }
}

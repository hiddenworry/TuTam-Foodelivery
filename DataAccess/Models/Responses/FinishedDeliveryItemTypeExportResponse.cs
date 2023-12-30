namespace DataAccess.Models.Responses
{
    public class FinishedDeliveryItemTypeExportResponse
    {
        public Guid DeliveryItemId { get; set; }

        public string Name { get; set; }

        public string Image { get; set; }

        public string Unit { get; set; }

        public double AssignedQuantity { get; set; }

        public double ExportedQuantity { get; set; }

        public List<SimpleStockUpdatedHistoryDetailResponse> Stocks { get; set; }
    }
}

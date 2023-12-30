namespace DataAccess.Models.Responses
{
    public class AidItemResponse
    {
        public Guid Id { get; set; }

        public double Quantity { get; set; }

        public double ExportedQuantity { get; set; }

        public double RealExportedQuantity { get; set; }

        public string Status { get; set; }

        public ItemResponse ItemResponse { get; set; }
    }
}

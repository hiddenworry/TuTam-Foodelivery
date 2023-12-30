namespace DataAccess.Models.Responses
{
    public class FinishedDeliveryItemTypeImportResponse
    {
        public Guid DeliveryItemId { get; set; }

        public string Name { get; set; }

        public string Image { get; set; }

        public string Unit { get; set; }

        public DateTime ConfirmedExpirationDate { get; set; }

        public double AssignedQuantity { get; set; }

        public double? ReceivedQuantity { get; set; }

        public double ImportedQuantity { get; set; }
    }
}

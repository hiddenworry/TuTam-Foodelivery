namespace DataAccess.Models.Responses
{
    public class DeliveryItemDetailsResponse
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public List<string> AttributeValues { get; set; }

        public string Image { get; set; }

        public string Note { get; set; }

        public int EstimatedExpirationDays { get; set; }

        public double MaximumTransportVolume { get; set; }

        public double Quantity { get; set; }

        public double? ReceivedQuantity { get; set; }

        public DateTime InitialExpirationDate { get; set; }

        public string Status { get; set; }

        public string Unit { get; set; }
    }
}

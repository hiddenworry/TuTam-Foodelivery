namespace DataAccess.Models.Requests
{
    public class ReceivedDeliveryItemRequest
    {
        public Guid DeliveryItemId { get; set; }

        public double Quantity { get; set; }

        public DateTime ExpirationDate { get; set; }

        public string? Note { get; set; }
    }
}

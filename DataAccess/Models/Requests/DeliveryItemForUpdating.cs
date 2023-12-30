namespace DataAccess.Models.Requests
{
    public class DeliveryItemForUpdating
    {
        public Guid DeliveryItemId { get; set; }

        public double Quantity { get; set; }
    }
}

namespace DataAccess.Models.Requests
{
    public class DeliveryItemsOfDeliveryRequestUpdatingRequest
    {
        public string ProofImage { get; set; }

        public List<DeliveryItemForUpdating> DeliveryItemForUpdatings { get; set; }
    }
}

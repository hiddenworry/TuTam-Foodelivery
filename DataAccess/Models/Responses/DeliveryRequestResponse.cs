namespace DataAccess.Models.Responses
{
    public class DeliveryRequestResponse
    {
        public Guid Id { get; set; }
        public DateTime CreatedDate { get; set; }

        //  public List<ItemResponse>? DeliveryItems { get; set; }

        public string Status { get; set; }

        public PickUpPointResponse? PickUpPoint { get; set; }

        public DeliveryPointResponse? DeliveryPoint { get; set; }

        public string DeliveryType { get; set; }

        public List<ItemResponse> ItemResponses { get; set; }
    }
}

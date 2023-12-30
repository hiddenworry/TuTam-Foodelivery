using DataAccess.Models.Requests;

namespace DataAccess.Models.Responses
{
    public class DeliveryRequestResponseDetails
    {
        public Guid Id { get; set; }

        public DateTime CreatedDate { get; set; }

        //  public List<ItemResponse>? DeliveryItems { get; set; }

        public string Status { get; set; }

        public PickUpPointResponse? PickUpPoint { get; set; }

        public DeliveryPointResponse? DeliveryPointResponse { get; set; }

        public string DeliveryType { get; set; }

        public List<DeliveryItemDetailsResponse> ItemResponses { get; set; }

        public ScheduledTime? CurrentScheduledTime { get; set; }

        public List<ScheduledTime>? ScheduledTimes { get; set; }

        public string? ProofImage { get; set; }

        public SimpleUserResponse Collaborator { get; set; }

        public string? CanceledReason { get; set; }
    }
}

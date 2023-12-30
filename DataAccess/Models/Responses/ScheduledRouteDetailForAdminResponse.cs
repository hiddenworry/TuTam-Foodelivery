using DataAccess.Models.Requests;

namespace DataAccess.Models.Responses
{
    public class ScheduledRouteDetailForAdminResponse
    {
        public Guid Id { get; set; }

        public int NumberOfDeliveryRequests { get; set; }

        public ScheduledTime ScheduledTime { get; set; }

        public List<SimpleDeliveryRequestResponse> OrderedDeliveryRequests { get; set; }

        public double TotalDistanceAsMeters { get; set; }

        public double TotalTimeAsSeconds { get; set; }

        public string BulkyLevel { get; set; }

        public string Type { get; set; }

        public string Status { get; set; }

        public SimpleUserResponse? AcceptedUser { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? AcceptedDate { get; set; }

        public DateTime? FinishedDate { get; set; }
    }
}

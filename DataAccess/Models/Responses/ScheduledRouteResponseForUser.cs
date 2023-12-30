using DataAccess.Models.Requests;

namespace DataAccess.Models.Responses
{
    public class ScheduledRouteResponseForUser
    {
        public Guid Id { get; set; }

        public int NumberOfDeliveryRequests { get; set; }

        public ScheduledTime ScheduledTime { get; set; }

        public List<string> OrderedAddresses { get; set; }

        public double TotalDistanceAsMeters { get; set; }

        public double TotalTimeAsSeconds { get; set; }

        public string BulkyLevel { get; set; }

        public string Type { get; set; }

        public string Status { get; set; }
    }
}

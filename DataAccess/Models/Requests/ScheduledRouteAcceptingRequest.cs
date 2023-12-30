namespace DataAccess.Models.Requests
{
    public class ScheduledRouteAcceptingRequest
    {
        public Guid ScheduledRouteId { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }
    }
}

namespace DataAccess.Models.Requests
{
    public class ScheduledRouteStartingRequest
    {
        public Guid ScheduledRouteId { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }
    }
}

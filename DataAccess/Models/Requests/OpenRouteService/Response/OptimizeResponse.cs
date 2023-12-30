namespace DataAccess.Models.Requests.OpenRouteService.Response
{
    public class OptimizeResponse
    {
        public List<UnassignedShipment> Unassigned { get; set; }

        public List<Route> Routes { get; set; }
    }
}

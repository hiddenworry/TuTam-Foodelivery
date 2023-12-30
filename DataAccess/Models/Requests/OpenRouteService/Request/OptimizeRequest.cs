namespace DataAccess.Models.Requests.OpenRouteService.Request
{
    public class OptimizeRequest
    {
        public List<Vehicle> Vehicles { get; set; }

        public List<Shipment> Shipments { get; set; }
    }
}

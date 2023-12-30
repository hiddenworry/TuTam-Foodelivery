namespace DataAccess.Models.Requests.OpenRouteService.Request
{
    public class Shipment
    {
        public List<int> Amount { get; set; }

        public Place Pickup { get; set; }

        public Place Delivery { get; set; }

        public List<int>? Skills { get; set; }
    }
}

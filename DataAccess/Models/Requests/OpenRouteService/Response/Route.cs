namespace DataAccess.Models.Requests.OpenRouteService.Response
{
    public class Route
    {
        public int Vehicle { get; set; }
        public int Cost { get; set; }
        public List<int> Amount { get; set; }
        public List<int> Delivery { get; set; }
        public List<int> Pickup { get; set; }
        public int Service { get; set; }
        public int Duration { get; set; }
        public int WaitingTime { get; set; }
        public List<Step> Steps { get; set; }
    }
}

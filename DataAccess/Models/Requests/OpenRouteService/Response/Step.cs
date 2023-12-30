namespace DataAccess.Models.Requests.OpenRouteService.Response
{
    public class Step
    {
        public string Type { get; set; }
        public List<double> Location { get; set; }
        public int Id { get; set; }
        public int Service { get; set; }
        public int WaitingTime { get; set; }
        public int Job { get; set; }
        public List<int> Load { get; set; }
        public long Arrival { get; set; }
        public int Duration { get; set; }
    }
}

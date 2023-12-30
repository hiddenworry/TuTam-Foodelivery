namespace DataAccess.Models.Requests.OpenRouteService.Request
{
    public class Vehicle
    {
        public int Id { get; set; }

        public string Profile { get; } = "cycling-electric";

        public List<double>? Start { get; set; }

        public List<double>? End { get; set; }

        public List<int> Capacity { get; set; }

        public List<int>? Skills { get; set; }

        public List<long>? time_window { get; set; }
    }
}

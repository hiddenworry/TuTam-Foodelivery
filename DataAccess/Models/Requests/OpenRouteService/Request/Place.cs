namespace DataAccess.Models.Requests.OpenRouteService.Request
{
    public class Place
    {
        public int Id { get; set; }

        public int Service { get; set; }

        public List<double> Location { get; set; }
    }
}

using DataAccess.Entities;

namespace DataAccess.Models.Requests.OpenRouteService.Response
{
    public class DeliverableBranches
    {
        public Branch? NearestBranch { get; set; }

        public List<Branch> NearbyBranches { get; set; }
    }
}

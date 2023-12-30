using DataAccess.EntityEnums;
using Microsoft.AspNetCore.Http;

namespace DataAccess.Models.Requests
{
    public class ActivityUpdatingRequest
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string? Address { get; set; }

        public List<double>? Location { get; set; }

        public DateTime EstimatedStartDate { get; set; }

        public DateTime EstimatedEndDate { get; set; }

        public DateTime? DeliveringDate { get; set; }

        public ActivityStatus Status { get; set; }

        public string Description { get; set; }

        public List<IFormFile>? Images { get; set; }

        public List<Guid> ActivityTypeIds { get; set; }

        public List<Guid>? BranchIds { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class ActivityRoleCreatingRequest
    {
        [Required]
        public Guid ActivityId { get; set; }

        [Required]
        public List<ActivityRoleRequest> ActivityRoleRequests { get; set; }
    }
}

using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities
{
    public class ScheduledRoute
    {
        [Key]
        public Guid Id { get; set; }

        public ScheduledRouteStatus Status { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? AcceptedDate { get; set; }

        public DateTime? FinishedDate { get; set; }

        public DateTime StartDate { get; set; }

        [ForeignKey(nameof(User))]
        public Guid? UserId { get; set; }

        public User? User { get; set; }

        public List<ScheduledRouteDeliveryRequest> ScheduledRouteDeliveryRequests { get; set; }
    }
}

using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities
{
    public class DonatedRequest
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Images { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? ConfirmedDate { get; set; }

        [StringLength(250, MinimumLength = 10)]
        public string Address { get; set; }

        [Required]
        public string Location { get; set; }

        public string ScheduledTimes { get; set; }

        public DonatedRequestStatus Status { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }

        public User User { get; set; }

        [ForeignKey(nameof(Activity))]
        public Guid? ActivityId { get; set; }

        public Activity? Activity { get; set; }

        public List<DonatedItem> DonatedItems { get; set; }

        public List<AcceptableDonatedRequest> AcceptableDonatedRequests { get; set; }

        public List<DeliveryRequest> DeliveryRequests { get; set; }
    }
}

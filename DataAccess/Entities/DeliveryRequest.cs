using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities
{
    public class DeliveryRequest
    {
        [Key]
        public Guid Id { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedDate { get; set; }

        public string ScheduledTimes { get; set; }

        public string? CurrentScheduledTime { get; set; }

        [ForeignKey(nameof(Branch))]
        public Guid BranchId { get; set; }

        public Branch Branch { get; set; }

        [ForeignKey(nameof(DonatedRequest))]
        public Guid? DonatedRequestId { get; set; }

        public DonatedRequest? DonatedRequest { get; set; }

        [ForeignKey(nameof(AidRequest))]
        public Guid? AidRequestId { get; set; }

        public AidRequest? AidRequest { get; set; }

        public string? ProofImage { get; set; }

        public List<ScheduledRouteDeliveryRequest> ScheduledRouteDeliveryRequests { get; set; }

        public List<DeliveryItem> DeliveryItems { get; set; }

        public DeliveryRequestStatus Status { get; set; }

        [StringLength(500, MinimumLength = 25)]
        public string? CanceledReason { get; set; }
    }
}

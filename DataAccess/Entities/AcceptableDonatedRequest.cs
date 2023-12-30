using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities
{
    public class AcceptableDonatedRequest
    {
        [ForeignKey(nameof(Branch))]
        public Guid BranchId { get; set; }

        public Branch Branch { get; set; }

        [ForeignKey(nameof(DonatedRequest))]
        public Guid DonatedRequestId { get; set; }

        public DonatedRequest DonatedRequest { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? ConfirmedDate { get; set; }

        [StringLength(500, MinimumLength = 25)]
        public string? RejectingReason { get; set; }

        public AcceptableDonatedRequestStatus Status { get; set; }
    }
}

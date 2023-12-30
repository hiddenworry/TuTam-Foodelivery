using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities
{
    public class AcceptableAidRequest
    {
        [ForeignKey(nameof(Branch))]
        public Guid BranchId { get; set; }

        public Branch Branch { get; set; }

        [ForeignKey(nameof(AidRequest))]
        public Guid AidRequestId { get; set; }

        public AidRequest AidRequest { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? ConfirmedDate { get; set; }

        [StringLength(500, MinimumLength = 25)]
        public string? RejectingReason { get; set; }

        public AcceptableAidRequestStatus Status { get; set; }
    }
}

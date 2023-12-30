using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities
{
    public class AidRequest
    {
        [Key]
        public Guid Id { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? ConfirmedDate { get; set; }

        [StringLength(250, MinimumLength = 10)]
        [Required]
        public string Address { get; set; }

        [Required]
        public string Location { get; set; }

        public string ScheduledTimes { get; set; }

        public AidRequestStatus Status { get; set; }

        public bool IsSelfShipping { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        public List<AidItem> AidItems { get; set; }

        [ForeignKey(nameof(CharityUnit))]
        public Guid? CharityUnitId { get; set; }

        public CharityUnit? CharityUnit { get; set; }

        [ForeignKey(nameof(Branch))]
        public Guid? BranchId { get; set; }

        public Branch? Branch { get; set; }

        public List<AcceptableAidRequest> AcceptableAidRequests { get; set; }

        public List<DeliveryRequest> DeliveryRequests { get; set; }

        public List<StockUpdatedHistoryDetail> StockUpdatedHistoryDetails { get; set; }
    }
}

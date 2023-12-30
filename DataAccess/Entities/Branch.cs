using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities
{
    public class Branch
    {
        [Key]
        public Guid Id { get; set; }

        [StringLength(100, MinimumLength = 5)]
        [Required]
        public string Name { get; set; }

        [StringLength(250, MinimumLength = 10)]
        public string Address { get; set; }

        public string Location { get; set; }

        public string Image { get; set; }

        [MinLength(50)]
        [Required]
        public string Description { get; set; }

        public DateTime CreatedDate { get; set; }

        public BranchStatus Status { get; set; }

        [ForeignKey(nameof(BranchAdmin))]
        public Guid BranchAdminId { get; set; }

        public User BranchAdmin { get; set; }

        public List<Stock> Stocks { get; set; }

        public List<StockUpdatedHistory> StockUpdatedHistories { get; set; }

        public List<ActivityBranch> ActivityBranches { get; set; }

        public List<AcceptableDonatedRequest> AcceptableDonatedRequests { get; set; }

        public List<AcceptableAidRequest> AcceptableAidRequests { get; set; }

        public List<DeliveryRequest> DeliveryRequests { get; set; }
    }
}

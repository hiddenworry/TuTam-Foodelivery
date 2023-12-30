using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities
{
    public class StockUpdatedHistory
    {
        [Key]
        public Guid Id { get; set; }

        public DateTime? CreatedDate { get; set; }

        public StockUpdatedHistoryType Type { get; set; }

        [ForeignKey(nameof(Branch))]
        public Guid BranchId { get; set; }

        public Branch Branch { get; set; }

        public List<StockUpdatedHistoryDetail> StockUpdatedHistoryDetails { get; set; }

        [Required]
        public Guid CreatedBy { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        public bool IsPrivate { get; set; }
    }
}

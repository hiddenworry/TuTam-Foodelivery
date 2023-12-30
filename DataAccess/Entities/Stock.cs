using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities
{
    public class Stock
    {
        [Key]
        public Guid Id { get; set; }

        public DateTime CreatedDate { get; set; }

        [Required]
        public DateTime ExpirationDate { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public double Quantity { get; set; }

        public StockStatus Status { get; set; }

        [StringLength(100)]
        public string? StockCode { get; set; }

        [ForeignKey(nameof(Item))]
        public Guid ItemId { get; set; }

        public Item Item { get; set; }

        [ForeignKey(nameof(Branch))]
        public Guid BranchId { get; set; }

        public Branch Branch { get; set; }

        [ForeignKey(nameof(User))]
        public Guid? UserId { get; set; }

        public User? User { get; set; }

        [ForeignKey(nameof(Activity))]
        public Guid? ActivityId { get; set; }

        public Activity? Activity { get; set; }

        public List<StockUpdatedHistoryDetail> StockUpdatedHistoryDetails { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities
{
    public class TargetProcess
    {
        [Required]
        [ForeignKey(nameof(Activity))]
        public Guid ActivityId { get; set; }

        [Required]
        [ForeignKey(nameof(Item))]
        public Guid ItemId { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public double Target { get; set; }

        [Required]
        [Range(1, double.MaxValue)]
        public double Process { get; set; }

        public Activity Activity { get; set; }

        public Item Item { get; set; }
    }
}

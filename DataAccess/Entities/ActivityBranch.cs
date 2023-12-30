using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Entities
{
    public class ActivityBranch
    {
        [Required]
        [ForeignKey(nameof(Activity))]
        public Guid ActivityId { get; set; }

        public Activity Activity { get; set; }

        [Required]
        [ForeignKey(nameof(Branch))]
        public Guid BranchId { get; set; }

        public Branch Branch { get; set; }
    }
}

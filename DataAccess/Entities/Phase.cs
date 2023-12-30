using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities
{
    public class Phase
    {
        [Key]
        public Guid Id { get; set; }

        [StringLength(100, MinimumLength = 5)]
        [Required]
        public string Name { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public DateTime EstimatedStartDate { get; set; }

        public DateTime EstimatedEndDate { get; set; }

        public PhaseStatus Status { get; set; }

        [ForeignKey(nameof(Activity))]
        public Guid ActivityId { get; set; }

        public Activity Activity { get; set; }

        public List<ActivityTask> ActivityTasks { get; set; }

        [Range(1, int.MaxValue)]
        public int Order { get; set; }
    }
}

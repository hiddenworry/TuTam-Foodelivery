using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities
{
    public class ActivityTask
    {
        [Key]
        public Guid Id { get; set; }

        [StringLength(100, MinimumLength = 5)]
        [Required]
        public string Name { get; set; }

        [StringLength(500, MinimumLength = 5)]
        [Required]
        public string Description { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public ActivityTaskStatus Status { get; set; }

        [ForeignKey(nameof(Phase))]
        public Guid PhaseId { get; set; }

        public Phase Phase { get; set; }

        public List<RoleTask> RoleTasks { get; set; }
    }
}

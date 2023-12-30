using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities
{
    public class ActivityRole
    {
        [Key]
        public Guid Id { get; set; }

        [StringLength(100, MinimumLength = 5)]
        [Required]
        public string Name { get; set; }

        [StringLength(500, MinimumLength = 1)]
        [Required]
        public string Description { get; set; }

        [Required]
        public bool IsDefault { get; set; }

        public ActivityRoleStatus Status { get; set; }

        [Required]
        [ForeignKey(nameof(Activity))]
        public Guid ActivityId { get; set; }

        public Activity Activity { get; set; }

        public List<RoleTask> RoleTasks { get; set; }

        public List<RoleMember> RoleMembers { get; set; }
    }
}

using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Entities
{
    public class RoleTask
    {
        [Required]
        [ForeignKey(nameof(ActivityRole))]
        public Guid ActivityRoleId { get; set; }

        public ActivityRole ActivityRole { get; set; }

        [Required]
        [ForeignKey(nameof(ActivityTask))]
        public Guid ActivityTaskId { get; set; }

        public ActivityTask ActivityTask { get; set; }
    }
}

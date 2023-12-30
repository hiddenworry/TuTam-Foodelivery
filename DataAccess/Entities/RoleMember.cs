using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Entities
{
    public class RoleMember
    {
        [Required]
        [ForeignKey(nameof(ActivityRole))]
        public Guid ActivityRoleId { get; set; }

        public ActivityRole ActivityRole { get; set; }

        [Required]
        [ForeignKey(nameof(ActivityMember))]
        public Guid ActivityMemberId { get; set; }

        public ActivityMember ActivityMember { get; set; }
    }
}

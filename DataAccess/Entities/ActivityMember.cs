using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using DataAccess.EntityEnums;

namespace DataAccess.Entities
{
    public class ActivityMember
    {
        [Key]
        public Guid Id { get; set; }

        public DateTime CreatedDate { get; set; }

        public ActivityMemberStatus Status { get; set; }

        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(Activity))]
        public Guid ActivityId { get; set; }

        public User User { get; set; }

        public Activity Activity { get; set; }

        public List<RoleMember> RoleMembers { get; set; }

        public DateTime? ConfirmedDate { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }
    }
}

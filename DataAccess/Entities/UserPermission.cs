using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities
{
    public class UserPermission
    {
        [Required]
        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }

        [Required]
        [ForeignKey(nameof(Permission))]
        public Guid PermissionId { get; set; }

        [Required]
        public UserPermissionStatus Status { get; set; }

        public User User { get; set; }

        public Permission Permission { get; set; }
    }
}

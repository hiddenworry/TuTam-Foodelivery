using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities
{
    public class RolePermission
    {
        [ForeignKey(nameof(Role))]
        public Guid RoleId { get; set; }

        [ForeignKey(nameof(Permission))]
        public Guid PermissionId { get; set; }

        [Required]
        public RolePermissionStatus Status { get; set; }

        public Role Role { get; set; }

        public Permission Permission { get; set; }
    }
}

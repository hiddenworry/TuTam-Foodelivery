using System.ComponentModel.DataAnnotations;

namespace DataAccess.Entities
{
    public class Role
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string DisplayName { get; set; }

        public List<User> Users { get; set; }

        public List<RolePermission> RolePermissions { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataAccess.Entities
{
    public class Permission
    {
        [Key]
        public Guid Id { get; set; }

        [StringLength(100, MinimumLength = 1)]
        [Required]
        public string Name { get; set; }

        [StringLength(100, MinimumLength = 1)]
        [Required]
        public string DisplayName { get; set; }

        public List<RolePermission> RolePermissions { get; set; }

        [JsonIgnore]
        public List<UserPermission> UserPermissions { get; set; }
    }
}

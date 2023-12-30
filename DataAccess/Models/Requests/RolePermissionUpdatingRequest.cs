using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class RolePermissionUpdatingRequest
    {
        [Required(ErrorMessage = "Vai trò không được để trống.")]
        public Guid RoleId { get; set; }

        [Required(ErrorMessage = "Quyền không được để trống.")]
        public Guid PermissionId { get; set; }

        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        [Range(0, 1, ErrorMessage = "Giá trị phải là 0 hoặc 1 PERMITTED hoặc BANED.")]
        public RolePermissionStatus Status { get; set; }
    }
}

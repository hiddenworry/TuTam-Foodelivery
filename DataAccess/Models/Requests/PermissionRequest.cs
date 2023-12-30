using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class PermissionRequest
    {
        [Required(ErrorMessage = "Quyền không được để trống.")]
        public Guid PermissionId { get; set; }

        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        [Range(0, 1, ErrorMessage = "Giá trị phải là 0 hoặc 1 PERMITTED hoặc BANED.")]
        public UserPermissionStatus Status { get; set; }
    }
}

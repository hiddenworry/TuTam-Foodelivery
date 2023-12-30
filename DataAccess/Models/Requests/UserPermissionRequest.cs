using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class UserPermissionRequest
    {
        [Required(ErrorMessage = "Người dùng không được để trống.")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Quyền dùng không được để trống.")]
        public List<PermissionRequest> PermissionRequests { get; set; }
    }
}

using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class NotificationUpdatingRequest
    {
        [Required(ErrorMessage = "Thông báo không được để trống.")]
        public List<Guid> NotificationIds { get; set; }

        [Required(ErrorMessage = "Status không được để trống.")]
        [Range(0, 1, ErrorMessage = "Giá trị phải là 0 hoặc 1, tương ứng với NEW hoặc SEEN.")]
        public NotificationStatus Status { get; set; }
    }
}

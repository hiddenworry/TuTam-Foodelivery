using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class TaskRequest
    {
        [StringLength(
            100,
            MinimumLength = 5,
            ErrorMessage = "Tên nhiệm vụ phải từ 5 đến 100 ký tự."
        )]
        [Required(ErrorMessage = "Tên nhiệm vụ không được để trống.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Mô tả nhiệm vụ không được để trống.")]
        [StringLength(
            500,
            MinimumLength = 5,
            ErrorMessage = "Mô tả nhiệm vụ phải từ 5 đến 500 ký tự."
        )]
        public string Description { get; set; }

        [Required(ErrorMessage = "Các vai trò tương ứng để đảm nhận nhiệm vụ này.")]
        public List<Guid> ActivityRoleIds { get; set; }
    }
}

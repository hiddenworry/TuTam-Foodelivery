using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class ActivityRoleUpdatingRequest
    {
        [Required]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Tên của vai trò không được để trống.")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Tên phải có từ 5 đến 100 kí tự.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Mô tả không được để trống.")]
        [StringLength(500, MinimumLength = 1, ErrorMessage = "Mô tả phải từ 1 đến 500 kí tự.")]
        public string Description { get; set; }

        public bool IsDefault { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class ActivityRoleRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Tên phải có từ 5 đến 100 kí tự.")]
        public string Name { get; set; }

        [StringLength(500, MinimumLength = 1, ErrorMessage = "Tên phải có từ 500 kí tự.")]
        [Required]
        public string Description { get; set; }

        [Required]
        public bool IsDefault { get; set; }
    }
}

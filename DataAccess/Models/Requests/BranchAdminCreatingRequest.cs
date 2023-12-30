using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class BranchAdminCreatingRequest
    {
        [Required(ErrorMessage = "Địa chỉ email không được trống.")]
        [DataType(DataType.EmailAddress, ErrorMessage = "Địa chỉ email không hợp lệ")]
        [StringLength(100, MinimumLength = 5)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được trống.")]
        [RegularExpression(
            @"^\+?\d{1,3}[-. \(\)]?\d{1,14}$",
            ErrorMessage = "Số điện thoại không hợp lệ."
        )]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Tên không được trống.")]
        [StringLength(50, MinimumLength = 5, ErrorMessage = "Tên phải có từ 8 đến 60 kí tự.")]
        public string FullName { get; set; }

        [Required]
        public IFormFile? Avatar { get; set; }

        [StringLength(
            250,
            MinimumLength = 10,
            ErrorMessage = "Địa chỉ phải có từ 10 đến 250 kí tự."
        )]
        public string? Address { get; set; }

        public double[]? Location { get; set; }
    }
}

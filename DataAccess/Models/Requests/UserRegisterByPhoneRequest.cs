using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class UserRegisterByPhoneRequest
    {
        [Required(ErrorMessage = "Số điện thoại không được trống.")]
        [RegularExpression(
            @"^\+?\d{1,3}[-. \(\)]?\d{1,14}$",
            ErrorMessage = "Số điện thoại không hợp lệ."
        )]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Tên không được để trống.")]
        [StringLength(50, MinimumLength = 5, ErrorMessage = "Tên phải có từ 5 đến 50 kí tự.")]
        public string FullName { get; set; }
    }
}

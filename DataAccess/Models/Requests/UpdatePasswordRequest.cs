using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class UpdatePasswordRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập đúng mật khẩu cũ.")]
        public string oldPassword { get; set; }

        [Required(ErrorMessage = "Mật khẩu mới không được trống.")]
        [StringLength(40, MinimumLength = 8, ErrorMessage = "Mật khẩu phải có từ 8 đến 40 kí tự.")]
        [RegularExpression(
            @"^(?=.*[A-Za-z])(?=.*\d).+$",
            ErrorMessage = "Mật khẩu mới phải có cả kí tự và số."
        )]
        public string newPassword { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "Mật khẩu không được trống.")]
        [StringLength(40, MinimumLength = 8, ErrorMessage = "Mật khẩu phải có từ 8 đến 40 kí tự.")]
        [RegularExpression(
            @"^(?=.*[A-Za-z])(?=.*\d).+$",
            ErrorMessage = "Mật khẩu phải có cả kí tự và số."
        )]
        public string Password { get; set; }

        [Required]
        public string VerifyCode { get; set; }
    }
}

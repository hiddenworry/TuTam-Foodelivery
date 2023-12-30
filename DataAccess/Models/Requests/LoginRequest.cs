using DataAccess.ModelsEnum;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests;

public class LoginRequest
{
    [Required(ErrorMessage = "Email/Số điện thoại là bắt buộc")]
    [MaxLength(50, ErrorMessage = "Email/Số điện thoại không thể vượt quá 50 Kí tự")]
    [RegularExpression(@"^\S+$", ErrorMessage = "Email/Số điện thoại không được bỏ trống")]
    public string UserName { get; set; }

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [MaxLength(100, ErrorMessage = "Mật khẩu không thể vượt quá 100 Kí tự")]
    [RegularExpression(@"^\S+$", ErrorMessage = "Mật khẩu không được bỏ trống")]
    public string Password { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn vai trò để đăng nhập")]
    public UserRole LoginRole { get; set; }
}

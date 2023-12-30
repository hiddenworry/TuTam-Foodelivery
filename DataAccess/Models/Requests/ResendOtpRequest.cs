using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class ResendOtpRequest
    {
        [Required]
        [RegularExpression(
            @"^\+?\d{1,3}[-. \(\)]?\d{1,14}$",
            ErrorMessage = "Số điện thoại không hợp lệ."
        )]
        public string Phone { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class LinkToEmailRequest
    {
        [DataType(DataType.EmailAddress, ErrorMessage = "Địa chỉ email không hợp lệ")]
        public string? Email { get; set; }

        [Required]
        public string VerifyCode { get; set; }
    }
}

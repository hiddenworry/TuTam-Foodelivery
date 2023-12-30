using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class ConfirmPostRequest
    {
        [Required(ErrorMessage = "Vui lòng chọn trạng thái.")]
        public bool isAccept { get; set; }

        [StringLength(500, ErrorMessage = "Lí do phải có từ 0 đến 500 kí tự.")]
        public string? reason { get; set; }
    }
}

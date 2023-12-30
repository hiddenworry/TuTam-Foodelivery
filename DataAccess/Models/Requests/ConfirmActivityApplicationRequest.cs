using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class ConfirmActivityApplicationRequest
    {
        [Required(ErrorMessage = "Vui lòng chọn trạng thái.")]
        public bool isAccept { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả nhiệm vụ không được vượt quá 500 ký tự.")]
        public string? reason { get; set; }
    }
}

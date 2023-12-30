using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class StockUpdateForUserDirectDonateRequest
    {
        [MinLength(1)]
        public List<DirectDonationRequest> DirectDonationRequests { get; set; }

        public Guid? UserId { get; set; }

        [StringLength(500, ErrorMessage = "Ghi chú phải có tối đa 500 kí tự.")]
        public string? Note { get; set; }

        [RegularExpression(
            @"^\+?\d{1,3}[-. \(\)]?\d{1,14}$",
            ErrorMessage = "Số điện thoại không hợp lệ."
        )]
        public string? Phone { get; set; }

        [StringLength(60, MinimumLength = 8, ErrorMessage = "Tên phải có từ 8 đến 60 kí tự.")]
        public string? FullName { get; set; }
    }
}

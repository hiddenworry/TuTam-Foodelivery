using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class ItemRequest
    {
        public Guid Id { get; set; }

        public List<string> Values { get; set; }

        [StringLength(500, ErrorMessage = "Ghi chú phải tối đa 500 kí tự.")]
        public string? Note { get; set; }

        [Range(2, 1825, ErrorMessage = "Ngày hết hạn ước tính phải từ 2 ngày đến tối đa 5 năm")]
        public int EstimatedExpirationDays { get; set; }

        [Required(ErrorMessage = "Hình ảnh không được để trống.")]
        public string ImageUrl { get; set; }

        [Range(1, 5000, ErrorMessage = "Khối lượng vận chuyển tối đa phải từ 1 đến 5000 đơn vị")]
        public double maximumTransportVolume { get; set; }

        public ItemStatus Status { get; set; }
    }
}

using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class ItemTemplateRequest
    {
        [Required(ErrorMessage = "Tên không được để trống.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Tên phải có từ 1 đến 100 kí tự.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        [Range(
            0,
            1,
            ErrorMessage = "Giá trị phải là 0 hoặc 1, tương ứng với INACTIVE hoặc ACTIVE."
        )]
        public ItemTemplateStatus Status { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        [Required(ErrorMessage = "Đơn vị không được để trống.")]
        public Guid ItemUnitId { get; set; }

        [Required(ErrorMessage = "Hình ảnh không được để trống.")]
        public string ImageUrl { get; set; }

        [Required(ErrorMessage = "Loại hàng hóa không được để trống.")]
        public Guid ItemcategoryId { get; set; }

        public List<AttributeRequest>? Attributes { get; set; }

        public List<ItemRequest>? ItemTemplates { get; set; }
    }
}

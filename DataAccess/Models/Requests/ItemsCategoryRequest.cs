using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class ItemsCategoryRequest
    {
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Tên phải từ 1 đến 100 kí tự")]
        [Required(ErrorMessage = "Tên của loại không được trống.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại(Food/Non-Food).")]
        public ItemCategoryType Type { get; set; }
    }
}

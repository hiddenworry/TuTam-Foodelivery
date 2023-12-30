using DataAccess.EntityEnums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class BranchCreatingRequest
    {
        [Required(ErrorMessage = "Tên của chi nhánh không được trống.")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Tên phải có từ 5 đến 100 kí tự.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Ảnh đại diện của chi nhánh không được trống.")]
        public IFormFile Image { get; set; }

        [Range(
            0,
            1,
            ErrorMessage = "Giá trị phải là 0 hoặc 1, tương ứng với ACTIVE hoặc INACTIVE."
        )]
        public BranchStatus Status { get; set; }

        [StringLength(
            250,
            MinimumLength = 10,
            ErrorMessage = "Địa chỉ phải có từ 10 đến 250 kí tự."
        )]
        [Required(ErrorMessage = "Địa chỉ không được để trống.")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Vị trí không được để trống.")]
        public double[]? Location { get; set; }

        [Required(ErrorMessage = "Người phụ trách của chi nhánh không được để trống.")]
        public Guid BranchAdminId { get; set; }

        [MinLength(50, ErrorMessage = "Mô tả phải từ 50 kí tự.")]
        public string Description { get; set; }
    }
}

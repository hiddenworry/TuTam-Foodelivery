using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class ImageUploadRequest
    {
        [Required(ErrorMessage = "Hình ảnh không được để trống")]
        [DataType(DataType.Upload)]
        public IFormFile image { get; set; }
    }
}

using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class PostCreatingRequest
    {
        [Required]
        [StringLength(
            2000,
            MinimumLength = 1,
            ErrorMessage = "Nội dung phải có từ 1 đến 2000 kí tự."
        )]
        public string Content { get; set; }

        [Required]
        public List<IFormFile> Images { get; set; }
    }
}

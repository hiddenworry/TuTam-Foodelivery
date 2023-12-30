using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class AttributeValueRequest
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Tên phải có từ 1 đến 100 kí tự.")]
        public string name { get; set; }
    }
}

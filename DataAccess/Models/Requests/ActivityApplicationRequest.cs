using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class ActivityApplicationRequest
    {
        public Guid ActivityId { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
        public string? Description { get; set; }

        public Guid RoleMemberId { get; set; }
    }
}

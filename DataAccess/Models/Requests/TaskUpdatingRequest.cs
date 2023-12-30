using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class TaskUpdatingRequest
    {
        [Required]
        public Guid Id { get; set; }

        [StringLength(
            100,
            MinimumLength = 5,
            ErrorMessage = "Tên nhiệm vụ phải từ 5 đến 100 ký tự."
        )]
        public string? Name { get; set; }

        [StringLength(
            500,
            MinimumLength = 5,
            ErrorMessage = "Mô tả nhiệm vụ phải từ 5 đến 500 ký tự."
        )]
        public string? Description { get; set; }

        public Guid? PhaseId { get; set; }

        [Range(0, 1, ErrorMessage = "Status phải từ 0(Start) and 1(End).")]
        public int? Status { get; set; }

        public List<Guid>? ActivityRoleIds { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class PharseCreatingRequest
    {
        [Required]
        public Guid ActivityId { get; set; }

        [Required(ErrorMessage = "Phase không được để trống.")]
        public List<PhaseRequest> phaseRequests { get; set; }
    }
}

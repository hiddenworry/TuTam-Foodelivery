using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class PhaseUpdatingRequest
    {
        public Guid Id { get; set; }
        public DateTime? EstimatedStartDate { get; set; }

        public DateTime? EstimatedEndDate { get; set; }

        [StringLength(100, MinimumLength = 5)]
        public string? Name { get; set; }

        public int? status { get; set; }
    }
}

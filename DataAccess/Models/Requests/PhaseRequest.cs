using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class PhaseRequest
    {
        [StringLength(100, MinimumLength = 5)]
        public string Name { get; set; }

        public DateTime EstimatedStartDate { get; set; }

        public DateTime EstimatedEndDate { get; set; }
    }
}

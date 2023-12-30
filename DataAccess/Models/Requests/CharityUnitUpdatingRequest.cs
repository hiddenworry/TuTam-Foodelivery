using Microsoft.AspNetCore.Http;

namespace DataAccess.Models.Requests
{
    public class CharityUnitUpdatingRequest
    {
        public string? Name { get; set; }

        public IFormFile? Image { get; set; }

        public IFormFile? LegalDocument { get; set; }

        public string? Address { get; set; }

        public double[]? Location { get; set; }

        public string? Description { get; set; }

        public bool? IsHeadquarter { get; set; }
    }
}

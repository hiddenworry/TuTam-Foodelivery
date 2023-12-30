using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class CharityUnitCreatingRequest
    {
        public string Email { get; set; }

        public string Phone { get; set; }

        public string Name { get; set; }

        [Required]
        public IFormFile Image { get; set; }

        [Required]
        public IFormFile LegalDocument { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        public double[]? Location { get; set; }

        public string Description { get; set; }

        public bool? IsHeadquarter { get; set; }
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class CharityUpdatingRequest
    {
        public Guid charityId { get; set; }
        public string Name { get; set; }

        public IFormFile Logo { get; set; }

        public string Description { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [FromForm]
        [Required]
        public IEnumerable<CharityUnitCreatingRequest> CharityUnits { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class ProofImage
    {
        [Required]
        public string Link { get; set; }
    }
}

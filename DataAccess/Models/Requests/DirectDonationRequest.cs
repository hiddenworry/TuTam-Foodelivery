using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class DirectDonationRequest
    {
        [Required]
        public Guid ItemId { get; set; }

        public int Quantity { get; set; }

        public string? Note { get; set; }

        public DateTime ExpirationDate { get; set; }
    }
}

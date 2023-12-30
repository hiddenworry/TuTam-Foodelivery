using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities
{
    public class CharityUnit
    {
        [Key]
        public Guid Id { get; set; }

        [StringLength(100, MinimumLength = 5)]
        [Required]
        public string Name { get; set; }

        [Required]
        public string Image { get; set; }

        [Required]
        public string LegalDocuments { get; set; }

        public DateTime CreatedDate { get; set; }

        [MinLength(50)]
        public string Description { get; set; }

        [StringLength(250, MinimumLength = 10)]
        [Required]
        public string Address { get; set; }

        [Required]
        public string Location { get; set; }

        public CharityUnitStatus Status { get; set; }

        [ForeignKey(nameof(Charity))]
        public Guid CharityId { get; set; }

        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }

        public Charity Charity { get; set; }

        public User User { get; set; }

        public List<AidRequest> AidRequests { get; set; }

        public bool? IsHeadquarter { get; set; }
    }
}

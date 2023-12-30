using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Entities
{
    public class Charity
    {
        [Key]
        public Guid Id { get; set; }

        [StringLength(100, MinimumLength = 5)]
        [Required]
        public string Name { get; set; }

        public DateTime CreatedDate { get; set; }

        [StringLength(100, MinimumLength = 5)]
        public string Email { get; set; }

        public CharityStatus Status { get; set; }

        [Required]
        public string Logo { get; set; }

        [MinLength(50)]
        [Required]
        public string Description { get; set; }

        public List<CharityUnit> CharityUnits { get; set; }
    }
}

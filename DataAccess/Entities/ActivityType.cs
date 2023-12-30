using System.ComponentModel.DataAnnotations;

namespace DataAccess.Entities
{
    public class ActivityType
    {
        [Key]
        public Guid Id { get; set; }

        [StringLength(100, MinimumLength = 5)]
        [Required]
        public string Name { get; set; }

        public List<ActivityTypeComponent> ActivityTypeComponents { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities
{
    public class ActivityTypeComponent
    {
        [Required]
        [ForeignKey(nameof(Activity))]
        public Guid ActivityId { get; set; }

        [Required]
        [ForeignKey(nameof(ActivityType))]
        public Guid ActivityTypeId { get; set; }

        public Activity Activity { get; set; }

        public ActivityType ActivityType { get; set; }
    }
}

using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using DataAccess.EntityEnums;

namespace DataAccess.Entities
{
    public class ActivityFeedback
    {
        [Key]
        public Guid Id { get; set; }

        [StringLength(500)]
        public string Content { get; set; }

        [Range(1, 5)]
        public double Rating { get; set; }

        public DateTime CreatedDate { get; set; }

        public ActivityFeedbackStatus Status { get; set; }

        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(Activity))]
        public Guid ActivityId { get; set; }

        public User User { get; set; }

        public Activity Activity { get; set; }
    }
}

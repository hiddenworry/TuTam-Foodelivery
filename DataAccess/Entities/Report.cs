using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DataAccess.Entities
{
    public class Report
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 10)]
        public string Title { get; set; }

        [StringLength(150, MinimumLength = 10)]
        [Required]
        public string Content { get; set; }

        public DateTime CreatedDate { get; set; }

        public ReportType Type { get; set; }

        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }

        public User User { get; set; }

        [JsonIgnore]
        public ScheduledRouteDeliveryRequest ScheduledRouteDeliveryRequest { get; set; }
    }
}

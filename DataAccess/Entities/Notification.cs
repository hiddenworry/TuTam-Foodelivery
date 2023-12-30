using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Entities
{
    public class Notification
    {
        [Key]
        public Guid Id { get; set; }

        [StringLength(100, MinimumLength = 1)]
        [Required]
        public string Name { get; set; }

        public string? Image { get; set; }

        [StringLength(500, MinimumLength = 1)]
        [Required]
        public string Content { get; set; }

        public DateTime CreatedDate { get; set; }

        public string ReceiverId { get; set; }

        public NotificationType Type { get; set; }

        public NotificationStatus Status { get; set; }

        public DataNotificationType DataType { get; set; }

        public Guid? DataId { get; set; }
    }
}

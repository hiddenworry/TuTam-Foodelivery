using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities
{
    public class PostComment
    {
        [Key]
        public Guid Id { get; set; }

        [StringLength(500, MinimumLength = 1)]
        [Required]
        public string Content { get; set; }

        public DateTime CreatedDate { get; set; }

        public PostCommentStatus Status { get; set; }

        [ForeignKey(nameof(Post))]
        public Guid PostId { get; set; }

        public Post Post { get; set; }

        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }

        public User User { get; set; }
    }
}

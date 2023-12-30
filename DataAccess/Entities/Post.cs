using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities
{
    public class Post
    {
        [Key]
        public Guid Id { get; set; }

        [StringLength(2000, MinimumLength = 1)]
        [Required]
        public string Content { get; set; }

        [Required]
        public string Images { get; set; }

        public DateTime CreatedDate { get; set; }

        public PostStatus Status { get; set; }

        [ForeignKey(nameof(Creater))]
        public Guid CreaterId { get; set; }

        public User Creater { get; set; }

        public List<PostComment> PostComments { get; set; }
    }
}

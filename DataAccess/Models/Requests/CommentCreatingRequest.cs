using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class CommentCreatingRequest
    {
        public Guid PostId { get; set; }

        [StringLength(500, MinimumLength = 1)]
        public string Content { get; set; }
    }
}

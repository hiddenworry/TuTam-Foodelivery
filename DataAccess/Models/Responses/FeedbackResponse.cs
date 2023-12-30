using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Responses
{
    public class FeedbackResponse
    {
        public Guid Id { get; set; }

        //[StringLength(500, ErrorMessage = "Nội dung phải dưới 500 kí tự.")]
        public string Content { get; set; }

        [Range(1, 5, ErrorMessage = "Rating phải từ 1 đến 5.")]
        public double Rating { get; set; }

        public DateTime CreatedDate { get; set; }

        public string Status { get; set; }
    }
}

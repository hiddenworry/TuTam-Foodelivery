namespace DataAccess.Models.Responses
{
    public class ActivityFeedbackResponse
    {
        public Guid? ActivityId { get; set; }

        public double? AverageStar { get; set; }

        public List<FeedbackResponse>? FeedbackResponses { get; set; }
    }
}

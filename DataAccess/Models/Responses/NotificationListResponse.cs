namespace DataAccess.Models.Responses
{
    public class NotificationListResponse
    {
        public int NotSeen { get; set; }

        public List<NotificationResponse> NotificationResponses { get; set; }
    }
}

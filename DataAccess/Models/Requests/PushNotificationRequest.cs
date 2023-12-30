namespace DataAccess.Models.Requests
{
    public class PushNotificationRequest
    {
        public string DeviceToken { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
    }
}

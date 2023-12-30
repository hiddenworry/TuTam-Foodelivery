namespace BusinessLogic.Utils.Notification
{
    public interface INotificationSignalSender
    {
        public Task SendNotification(string message);
    }
}

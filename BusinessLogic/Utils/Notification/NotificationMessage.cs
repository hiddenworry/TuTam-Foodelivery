namespace BusinessLogic.Utils.Notification
{
    public class NotificationMessage
    {
        public static string NOT_CONFIRM_TO_BECOME_COLLABORATOR =
            "Đơn xin được thực hiện yêu cầu vận chuyển của bạn đã bị từ chối vì lí do {0}";

        public static string BuildTaskAssignationMessage(string reason)
        {
            return string.Format(NOT_CONFIRM_TO_BECOME_COLLABORATOR, reason);
        }
    }
}

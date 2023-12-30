using DataAccess.Models.Requests;

namespace BusinessLogic.Utils.FirebaseService
{
    public interface IFirebaseNotificationService
    {
        Task<bool> PushNotification(PushNotificationRequest request);
    }
}

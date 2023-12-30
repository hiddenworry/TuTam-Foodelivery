using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using FirebaseAdmin;
using FCM = FirebaseAdmin.Messaging;
using FirebaseAdmin.Messaging;
using DataAccess.Models.Requests;

namespace BusinessLogic.Utils.FirebaseService.Implements
{
    public class FirebaseNotificationService : IFirebaseNotificationService
    {
        private readonly string _credentialFilePath;
        private readonly string _bucketName;

        public FirebaseNotificationService(IConfiguration configuration)
        {
            _credentialFilePath = configuration.GetValue<string>("Firebase:CredentialFilePath");
            _bucketName = configuration.GetValue<string>("Firebase:BucketName");
        }

        public async Task<bool> PushNotification(PushNotificationRequest request)
        {
            try
            {
                var credential = GoogleCredential.FromFile(_credentialFilePath);
                if (FirebaseApp.DefaultInstance == null)
                {
                    FirebaseApp.Create(new AppOptions { Credential = credential });
                }

                var message = new Message
                {
                    Token = request.DeviceToken,
                    Notification = new FCM.Notification
                    {
                        Title = request.Title,
                        Body = request.Message
                    },
                };

                var result = await FirebaseMessaging.DefaultInstance.SendAsync(message);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

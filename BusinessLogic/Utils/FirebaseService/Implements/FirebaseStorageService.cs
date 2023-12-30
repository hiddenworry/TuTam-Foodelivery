using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Utils.FirebaseService.Implements
{
    public class FirebaseStorageService : IFirebaseStorageService
    {
        private readonly string _credentialFilePath;
        private readonly string _bucketName;
        private readonly ILogger<FirebaseStorageService> _logger;

        public FirebaseStorageService(
            IConfiguration configuration,
            ILogger<FirebaseStorageService> logger
        )
        {
            _credentialFilePath = configuration.GetValue<string>("Firebase:CredentialFilePath");
            _bucketName = configuration.GetValue<string>("Firebase:BucketName");
            _logger = logger;
        }

        public async Task<string> UploadImageToFirebase(Stream imageStream, string imageName)
        {
            var credential = GoogleCredential.FromFile(_credentialFilePath);
            var storageClient = StorageClient.Create(credential);
            var obj = await storageClient.UploadObjectAsync(
                _bucketName,
                imageName,
                null,
                imageStream
            );
            return obj.MediaLink;
        }

        public string GetImageDownloadUrl(string imageName)
        {
            var credential = GoogleCredential.FromFile(_credentialFilePath);
            var storageClient = StorageClient.Create(credential);

            var storageObject = storageClient.GetObject(_bucketName, imageName);
            return storageObject.MediaLink;
        }

        public bool DeleteImage(string filePath)
        {
            try
            {
                var credential = GoogleCredential.FromFile(_credentialFilePath);
                var storageClient = StorageClient.Create(credential);
                // get image name
                var fileName = GetObjectNameFromImageUrl(filePath);
                storageClient.DeleteObject(_bucketName, fileName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred: {ErrorMessage}", ex.Message);
                return false;
            }
        }

        public string GetObjectNameFromImageUrl(string imageUrl)
        {
            Uri uri = new Uri(imageUrl);
            string[] segments = uri.Segments;
            string objectName = segments[segments.Length - 1];
            if (objectName.EndsWith("/"))
            {
                objectName = objectName.Remove(objectName.Length - 1);
            }

            return objectName;
        }

        public async Task<bool> DeleteImageAsync(string filePath)
        {
            try
            {
                var credential = GoogleCredential.FromFile(_credentialFilePath);
                var storageClient = StorageClient.Create(credential);
                // get image name
                var fileName = GetObjectNameFromImageUrl(filePath);
                await storageClient.DeleteObjectAsync(_bucketName, fileName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred: {ErrorMessage}", ex.Message);
                return false;
            }
        }
    }
}

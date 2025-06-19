using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace IncanaPortfolio.Api.Services
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(IFormFile imageFile);
    }

    public class GoogleCloudStorageService : IStorageService
    {
        private readonly StorageClient _storageClient;
        private readonly string _bucketName;

        public GoogleCloudStorageService(ISecretManagerService secretManager)
        {
            // Get all necessary configuration from the secret manager service
            _bucketName = secretManager.GetSecret("incana-portfolio-media");
            var credentialsJson = secretManager.GetSecret("incana-portfolio-serv-acc");

            try
            {
                var credentials = GoogleCredential.FromJson(credentialsJson);
                _storageClient = StorageClient.Create(credentials);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create Google Storage client from credentials.", ex);
            }
        }

        public async Task<string> UploadFileAsync(IFormFile imageFile)
        {
            var objectName = $"{Guid.NewGuid()}-{imageFile.FileName}";

            using (var memoryStream = new MemoryStream())
            {
                await imageFile.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                var uploadedObject = await _storageClient.UploadObjectAsync(
                    bucket: _bucketName,
                    objectName: objectName,
                    contentType: imageFile.ContentType,
                    source: memoryStream
                );

                return uploadedObject.MediaLink;
            }
        }
    }
}

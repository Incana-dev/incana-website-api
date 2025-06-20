using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Net.Http; // Required for HttpMethod
using System.Threading.Tasks;

namespace IncanaPortfolio.Api.Services
{
    public interface IStorageService
    {
        Task<(string signedUrl, string objectName)> UploadFileAsync(IFormFile imageFile);
        string GetSignedUrlForObject(string objectName);
    }

    public class GoogleCloudStorageService : IStorageService
    {
        private readonly StorageClient _storageClient;
        private readonly GoogleCredential _credential;
        private readonly string _bucketName;

        public GoogleCloudStorageService(ISecretManagerService secretManager)
        {
            _bucketName = secretManager.GetSecret("incana-portfolio-media");
            var credentialsJson = secretManager.GetSecret("incana-portfolio-serv-acc");

            try
            {
                _credential = GoogleCredential.FromJson(credentialsJson);
                _storageClient = StorageClient.Create(_credential);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create Google Storage client from credentials.", ex);
            }
        }

        // This is the single, correct implementation of the interface method.
        public async Task<(string signedUrl, string objectName)> UploadFileAsync(IFormFile imageFile)
        {
            var objectName = $"{Guid.NewGuid()}-{imageFile.FileName}";

            using (var memoryStream = new MemoryStream())
            {
                await imageFile.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                await _storageClient.UploadObjectAsync(
                    bucket: _bucketName,
                    objectName: objectName,
                    contentType: imageFile.ContentType,
                    source: memoryStream
                );
            }

            // Create a short-lived URL for immediate preview after upload
            var urlSigner = UrlSigner.FromCredential(_credential);
            string signedUrl = urlSigner.Sign(
                bucket: _bucketName,
                objectName: objectName,
                duration: TimeSpan.FromMinutes(10), // Short duration for preview
                httpMethod: HttpMethod.Get
            );

            // Return both the preview URL and the permanent object name
            return (signedUrl, objectName);
        }

        public string GetSignedUrlForObject(string objectName)
        {
            var urlSigner = UrlSigner.FromCredential(_credential);
            string signedUrl = urlSigner.Sign(
                bucket: _bucketName,
                objectName: objectName,
                duration: TimeSpan.FromHours(2),
                httpMethod: HttpMethod.Get
            );

            return signedUrl;
        }
    }
}
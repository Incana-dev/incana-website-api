using Google.Cloud.SecretManager.V1;
using System;

namespace IncanaPortfolio.Api.Services
{

    public interface ISecretManagerService
    {
        string GetSecret(string secretId);
    }

    public class GoogleSecretManagerService : ISecretManagerService
    {
        private readonly SecretManagerServiceClient _client;
        private const string ProjectId = "incanaportfolio";

        public GoogleSecretManagerService()
        {
            _client = SecretManagerServiceClient.Create();
        }

        public string GetSecret(string secretId)
        {
            // Use "latest" as the version for simplicity and best practice.
            var secretVersionName = new SecretVersionName(ProjectId, secretId, "latest");

            AccessSecretVersionResponse result = _client.AccessSecretVersion(secretVersionName);

            // Convert the payload to a string.
            return result.Payload.Data.ToStringUtf8();
        }
    }
}

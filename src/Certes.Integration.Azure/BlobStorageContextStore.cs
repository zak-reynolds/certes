using Certes.Json;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Certes.Integration.Azure
{
    public class BlobStorageContextStore : IContextStore
    {
        private static readonly JsonSerializerSettings jsonSettings = JsonUtil.CreateSettings();
        private const string ContainerName = "certes";
        private CloudBlobContainer blobContainer;
        private readonly StorageOptions options;
        private string leaseId = null;

        private CloudBlobContainer BlobContainer
        {
            get
            {
                if (blobContainer == null)
                {
                    var credentials = new StorageCredentials(options.AccountName, options.AccountName);
                    var storageAccount = new CloudStorageAccount(credentials, true);
                    var blobClient = storageAccount.CreateCloudBlobClient();
                    this.blobContainer = blobClient.GetContainerReference(ContainerName);
                }

                return this.blobContainer;
            }
        }
        public BlobStorageContextStore(IOptions<StorageOptions> options)
        {
            this.options = options.Value;
        }

        public async Task<CertesContext> Load(bool exclusive = false)
        {
            var container = BlobContainer;
            await container.CreateIfNotExistsAsync();
            var blob = container.GetBlockBlobReference("context.json");

            if (exclusive)
            {
                leaseId = await blob.AcquireLeaseAsync(TimeSpan.FromSeconds(60));
                if (string.IsNullOrWhiteSpace(leaseId))
                {
                    return null;
                }
            }

            if (!await blob.ExistsAsync())
            {
                var ctx = new CertesContext();
                var json = JsonConvert.SerializeObject(ctx, Formatting.None, jsonSettings);
                await blob.UploadTextAsync(json, Encoding.UTF8, AccessCondition.GenerateLeaseCondition(leaseId), null, null);
                return ctx;
            }
            else
            {
                using (var stream = await blob.OpenReadAsync())
                using (var reader = new StreamReader(stream))
                {
                    var json = await reader.ReadToEndAsync();
                    return JsonConvert.DeserializeObject<CertesContext>(json, jsonSettings);
                }
            }
        }

        public async Task Save(CertesContext context, bool release = false)
        {
            var container = BlobContainer;
            var blob = container.GetBlockBlobReference("context.json");

            var json = JsonConvert.SerializeObject(context, Formatting.None, jsonSettings);
            await blob.UploadTextAsync(json);

            if (release && !string.IsNullOrWhiteSpace(leaseId))
            {
                await blob.ReleaseLeaseAsync(AccessCondition.GenerateLeaseCondition(leaseId));
            }
        }
    }
}

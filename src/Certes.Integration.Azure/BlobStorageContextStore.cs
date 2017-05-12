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
using Certes.Acme;

namespace Certes.Integration.Azure
{
    public class BlobStorageContextStore : IContextStore
    {
        private static readonly JsonSerializerSettings jsonSettings = JsonUtil.CreateSettings();
        private const string ContainerName = "certes";
        private CloudBlobContainer blobContainer;
        private readonly StorageOptions options;

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

        public async ValueTask<AcmeAccount> GetOrCreate(Func<ValueTask<AcmeAccount>> provider)
        {
            var container = BlobContainer;
            var blob = container.GetBlockBlobReference("account.json");

            AcmeAccount account = null;
            if (await blob.ExistsAsync())
            {
                using (var stream = await blob.OpenReadAsync())
                using (var reader = new StreamReader(stream))
                {
                    var json = await reader.ReadToEndAsync();
                    account = JsonConvert.DeserializeObject<AcmeAccount>(json, jsonSettings);
                }
            }

            if (account == null)
            {
                account = await provider.Invoke();
                var json = JsonConvert.SerializeObject(account, Formatting.None, jsonSettings);
                await blob.UploadTextAsync(json);
            }

            return account;
        }

        public async ValueTask<AcmeResult<Authorization>> Get(AuthorizationIdentifier identifier)
        {
            var container = BlobContainer;
            var blob = container.GetBlockBlobReference($"authz/{identifier.Type}/{identifier.Value}.json");

            if (await blob.ExistsAsync())
            {
                using (var stream = await blob.OpenReadAsync())
                using (var reader = new StreamReader(stream))
                {
                    var json = await reader.ReadToEndAsync();
                    return JsonConvert.DeserializeObject<AcmeResult<Authorization>>(json, jsonSettings);
                }
            }

            return null;
        }

        public async Task Save(AcmeResult<Authorization> authorization)
        {
            var identifier = authorization.Data.Identifier;
            var container = BlobContainer;
            var blob = container.GetBlockBlobReference($"authz/{identifier.Type}/{identifier.Value}.json");

            var json = JsonConvert.SerializeObject(authorization, Formatting.None, jsonSettings);
            await blob.UploadTextAsync(json);

        }
    }
}

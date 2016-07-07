using Microsoft.Extensions.Options;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Certes.Azure
{
    public class SslBinding
    {
        public string HostName { get; set; }
        public string CertificateThumbprint { get; set; }
        public DateTimeOffset CertificateExpires { get; set; }
    }

    public class AzureWebAppManagementOptions
    {
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string SubscriptionId { get; set; }
        public string ResourceGroup { get; set; }
        public string Name { get; set; }
    }

    public interface ISslBindingManager
    {
        Task<IList<SslBinding>> GetHostNames();
    }
}

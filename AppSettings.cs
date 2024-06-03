
namespace AzureDnsApiSandBox
 
{
    public class AppSettings
    {
        public AzureDNSCreds AzureDNSCreds { get; set; } = new();
        public string ZoneName { get; set; } = string.Empty;
    }
    public class AzureDNSCreds
    {
        public string TenantId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string SubscriptionID { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
    }
}
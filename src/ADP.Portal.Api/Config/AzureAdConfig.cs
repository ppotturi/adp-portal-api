namespace ADP.Portal.Api.Config
{
    public class AzureAdConfig
    {
        public required string TenantId { get; set; }
        public required string ClientId { get; set; }
        public required string ClientSecret { get; set;}
    }
}

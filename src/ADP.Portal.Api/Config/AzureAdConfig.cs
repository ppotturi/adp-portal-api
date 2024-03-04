namespace ADP.Portal.Api.Config
{
    public class AzureAdConfig
    {
        public required string TenantId { get; set; }
        public required string SpClientId { get; set; }
        public required string SpClientSecret { get; set; }
        public required string SpObjectId { get; set; }
    }
}

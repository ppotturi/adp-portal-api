namespace ADP.Portal.Api.Config
{
    public class AdoConfig
    {
        public required string OrganizationUrl { get; set; }
        public required bool UsePatToken { get; set; }
        public string? PatToken { get; set; }
    }
}

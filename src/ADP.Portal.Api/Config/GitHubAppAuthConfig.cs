namespace ADP.Portal.Api.Config
{
    public class GitHubAppAuthConfig
    {
        public required string Owner { get; set; }
        public required string AppName { get; set; }
        public required int AppId { get; set; }
        public required string PrivateKeyBase64 { get; set; }
    }
}

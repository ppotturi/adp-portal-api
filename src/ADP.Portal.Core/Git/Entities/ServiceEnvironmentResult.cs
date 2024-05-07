namespace ADP.Portal.Core.Git.Entities
{
    public class ServiceEnvironmentResult : FluxConfigResult
    {
        public FluxEnvironment? Environment { get; set; }

        public required string FluxTemplatesVersion { get; set; }
    }
}

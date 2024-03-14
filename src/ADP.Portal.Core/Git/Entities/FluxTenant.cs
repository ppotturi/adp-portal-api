namespace ADP.Portal.Core.Git.Entities
{
    public class FluxTenant
    {
        public required List<FluxEnvironment> Environments { get; set; }
        public List<FluxConfig> ConfigVariables { get; private set; } = [];
    }
}

namespace ADP.Portal.Core.Git.Entities
{
    public class FluxEnvironment
    {
        public required string Name { get; set; }
        public List<FluxConfig> ConfigVariables { get; private set; } = [];
    }
}

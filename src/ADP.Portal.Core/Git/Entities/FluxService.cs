namespace ADP.Portal.Core.Git.Entities
{
    public class FluxService
    {
        public required string Name { get; set; }

        public required FluxServiceType Type { get; set; }

        public required List<string> Environments { get; set; }

        public List<FluxConfig> Tokens { get; set; } = [];
    }

    public enum FluxServiceType
    {
        Frontend,
        Backend
    }
}
namespace ADP.Portal.Core.Git.Entities
{
    public class FluxTeam
    {
        public required string ServiceCode { get; set; }

        public required List<FluxService> Services { get; set; }

        public List<FluxConfig> AdditionalConfig { get; set; } = [];
    }
}

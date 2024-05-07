namespace ADP.Portal.Api.Models.Flux
{
    public sealed class TeamConfigRequest
    {
        public required string ProgrammeName { get; set; }
        public required string ServiceCode { get; set; }
        public required string TeamName { get; set; }
        public required List<FluxService> Services { get; set; }
        public Dictionary<string, string> ConfigVariables { get; set; } = [];
    }
}

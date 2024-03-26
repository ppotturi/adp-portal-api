namespace ADP.Portal.Api.Models.Flux
{
    public class FluxService
    {
        public required string Name { get; set; }
        public required bool IsFrontend { get; set; }
        public List<string> Environments { get; set; } = [];
        public Dictionary<string, string> ConfigVariables { get; set; } = [];
    }
}
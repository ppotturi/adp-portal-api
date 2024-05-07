namespace ADP.Portal.Api.Models.Flux
{
    public sealed class ServiceConfigRequest
    {
        public required string Name { get; set; }
        public required bool IsFrontend { get; set; }
        public bool IsHelmOnly { get; set; } = false;
        public List<string>? Environments { get; set; }
        public Dictionary<string, string>? ConfigVariables { get; set; }
    }
}

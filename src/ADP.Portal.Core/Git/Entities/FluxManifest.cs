namespace ADP.Portal.Core.Git.Entities
{
    public class FluxManifest
    {
        public required bool Generate { get; set; }
        public string? GeneratedVersion { get; set; }
    }
}
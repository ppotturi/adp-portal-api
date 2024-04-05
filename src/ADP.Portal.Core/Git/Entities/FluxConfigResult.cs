namespace ADP.Portal.Core.Git.Entities
{
    public class FluxConfigResult
    {
        public bool IsConfigExists { get; set; } = true;

        public List<string> Errors { get; set; } = [];
    }
}

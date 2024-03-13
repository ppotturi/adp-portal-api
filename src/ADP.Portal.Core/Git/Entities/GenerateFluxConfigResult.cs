namespace ADP.Portal.Core.Git.Entities
{
    public class GenerateFluxConfigResult
    {
        public bool IsConfigExists { get; set; }= true;

        public List<String> Errors { get; set; } = [];
    }
}

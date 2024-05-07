namespace ADP.Portal.Core.Git.Entities
{
    public class GitRepo
    {
        public required string Name { get; set; }
        public required string Reference { get; set; }
        public required string Organisation { get; set; }
    }
}

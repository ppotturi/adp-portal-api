namespace ADP.Portal.Core.Git.Entities;

public record GitHubOptions
{
    public required string Organisation { get; set; }
    public List<string> TeamDenyList { get; set; } = [];
    public required string AdminLogin { get; set; }
}
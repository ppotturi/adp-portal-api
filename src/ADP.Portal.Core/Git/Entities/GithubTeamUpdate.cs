namespace ADP.Portal.Core.Git.Entities;

public sealed record GithubTeamUpdate
{
    public required string Name { get; set; }
    public required IEnumerable<string>? Members { get; set; }
    public required IEnumerable<string>? Maintainers { get; set; }
    public required string? Description { get; set; }
    public required bool? IsPublic { get; set; }
}
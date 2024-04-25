namespace ADP.Portal.Core.Git.Entities;

public sealed record GithubTeamDetails
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required IEnumerable<string> Members { get; init; }
    public required IEnumerable<string> Maintainers { get; init; }
    public required string Description { get; init; }
    public required bool IsPublic { get; init; }
    public required string Slug { get; init; }
}
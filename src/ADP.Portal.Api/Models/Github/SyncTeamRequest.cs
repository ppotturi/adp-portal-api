namespace ADP.Portal.Api.Models.Github;

public sealed class SyncTeamRequest
{
    public string? Description { get; set; }
    public bool? IsPublic { get; set; }
    public IEnumerable<string>? Members { get; set; }
    public IEnumerable<string>? Maintainers { get; set; }
}
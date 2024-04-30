using ADP.Portal.Core.Git.Entities;

namespace ADP.Portal.Core.Git.Services;

public interface IGitHubService
{
    Task<GithubTeamDetails?> SyncTeamAsync(GithubTeamUpdate team, CancellationToken cancellationToken);
}
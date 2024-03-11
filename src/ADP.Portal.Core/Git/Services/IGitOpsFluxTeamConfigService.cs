using ADP.Portal.Core.Git.Entities;

namespace ADP.Portal.Core.Git.Services
{
    public interface IGitOpsFluxTeamConfigService
    {
        Task GenerateFluxTeamConfig(GitRepo gitRepo, string teamName, string? serviceName = null);
    }
}

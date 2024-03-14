using ADP.Portal.Core.Git.Entities;

namespace ADP.Portal.Core.Git.Services
{
    public interface IGitOpsFluxTeamConfigService
    {
        Task<GenerateFluxConfigResult> GenerateFluxTeamConfig(GitRepo gitRepo, GitRepo gitRepoFluxServices, string tenantName, string teamName, string? serviceName = null);
    }
}

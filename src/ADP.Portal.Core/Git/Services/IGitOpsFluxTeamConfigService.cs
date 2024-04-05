using ADP.Portal.Core.Git.Entities;

namespace ADP.Portal.Core.Git.Services
{
    public interface IGitOpsFluxTeamConfigService
    {
        Task<T?> GetFluxConfigAsync<T>(GitRepo gitRepo, string? tenantName = null, string? teamName = null);

        Task<FluxConfigResult> CreateFluxConfigAsync(GitRepo gitRepo, string teamName, FluxTeamConfig fluxTeamConfig);

        Task<FluxConfigResult> UpdateFluxConfigAsync(GitRepo gitRepo, string teamName, FluxTeamConfig fluxTeamConfig);

        Task<GenerateFluxConfigResult> GenerateFluxTeamConfigAsync(GitRepo gitRepo, GitRepo gitRepoFluxServices, string tenantName, string teamName, string? serviceName = null);
    }
}

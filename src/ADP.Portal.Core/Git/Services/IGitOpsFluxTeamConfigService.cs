using ADP.Portal.Core.Git.Entities;

namespace ADP.Portal.Core.Git.Services
{
    public interface IGitOpsFluxTeamConfigService
    {
        Task<T?> GetConfigAsync<T>(GitRepo gitRepo, string? tenantName = null, string? teamName = null);

        Task<FluxConfigResult> CreateConfigAsync(GitRepo gitRepo, string teamName, FluxTeamConfig fluxTeamConfig);

        Task<FluxConfigResult> UpdateConfigAsync(GitRepo gitRepo, string teamName, FluxTeamConfig fluxTeamConfig);

        Task<GenerateFluxConfigResult> GenerateConfigAsync(GitRepo gitRepo, GitRepo gitRepoFluxServices, string tenantName, string teamName, string? serviceName = null);

        Task<FluxConfigResult> AddFluxServiceAsync(GitRepo gitRepo, string teamName, FluxService fluxService);
    }
}

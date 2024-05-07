using ADP.Portal.Core.Git.Entities;

namespace ADP.Portal.Core.Git.Services
{
    public interface IFluxTeamConfigService
    {
        Task<T?> GetConfigAsync<T>(string? tenantName = null, string? teamName = null);

        Task<FluxConfigResult> CreateConfigAsync(string teamName, FluxTeamConfig fluxTeamConfig);

        Task<GenerateManifestResult> GenerateManifestAsync(string tenantName, string teamName, string? serviceName = null, string? environment = null);

        Task<FluxConfigResult> AddServiceAsync(string teamName, FluxService fluxService);

        Task<ServiceEnvironmentResult> GetServiceEnvironmentAsync(string teamName,string serviceName, string environment);

        Task<FluxConfigResult> AddServiceEnvironmentAsync(string teamName, string serviceName, string environment);

        Task<FluxConfigResult> UpdateServiceEnvironmentManifestAsync(string teamName, string serviceName, string environment, bool generate);
    }
}

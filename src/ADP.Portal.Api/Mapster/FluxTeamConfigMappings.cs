using ADP.Portal.Api.Models.Flux;
using Mapster;

namespace ADP.Portal.Api.Mapster
{
    public static class FluxTeamConfigMappings
    {
        public static void Configure()
        {
            TypeAdapterConfig<FluxService, Core.Git.Entities.FluxService>.NewConfig()
                .Map(dest => dest.Environments, opt => opt.Environments.Select(x => new Core.Git.Entities.FluxEnvironment
                {
                    Name = x.ToLower(),
                    Manifest = new Core.Git.Entities.FluxManifest() { Generate = true }
                }))
                .Map(dest => dest.Type, opt => opt.IsFrontend ? Core.Git.Entities.FluxServiceType.Frontend : Core.Git.Entities.FluxServiceType.Backend);

            TypeAdapterConfig<ServiceConfigRequest, Core.Git.Entities.FluxService>.NewConfig()
                .Map(dest => dest.Environments, opt => (opt.Environments ?? new()).Select(x => new Core.Git.Entities.FluxEnvironment
                {
                    Name = x.ToLower(),
                    Manifest = new Core.Git.Entities.FluxManifest() { Generate = true }
                }))
                .Map(dest => dest.Type, opt => DetermineFluxServiceType(opt.IsFrontend, opt.IsHelmOnly))
                .Map(dest => dest.ConfigVariables, opt => opt.ConfigVariables ?? new());
        }

        private static Core.Git.Entities.FluxServiceType DetermineFluxServiceType(bool isFrontend, bool isHelmOnly)
        {
            if (isFrontend)
            {
                return isHelmOnly ? Core.Git.Entities.FluxServiceType.HelmOnly : Core.Git.Entities.FluxServiceType.Frontend;
            }
            else
            {
                return Core.Git.Entities.FluxServiceType.Backend;
            }
        }
    }
}

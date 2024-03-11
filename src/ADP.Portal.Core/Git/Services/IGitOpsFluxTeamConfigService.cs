using ADP.Portal.Core.Git.Entities;

namespace ADP.Portal.Core.Git.Services
{
    public interface IGitOpsFluxTeamConfigService
    {
        Task GenerateFluxTeamConfig(string teamName, GitRepo gitRepo);
    }
}

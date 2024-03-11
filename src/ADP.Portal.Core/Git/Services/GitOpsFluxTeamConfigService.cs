using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ADP.Portal.Core.Git.Services
{
    public class GitOpsFluxTeamConfigService : IGitOpsFluxTeamConfigService
    {
        private readonly IGitOpsConfigRepository gitOpsConfigRepository;

        public GitOpsFluxTeamConfigService(IGitOpsConfigRepository gitOpsConfigRepository, ILogger<GitOpsFluxTeamConfigService> logger)
        {
            this.gitOpsConfigRepository = gitOpsConfigRepository;
        }
        public async Task GenerateFluxTeamConfig(string teamName, GitRepo gitRepo)
        {
            var fileName = "";
            var teamConfig = await gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(fileName, gitRepo);

            await Task.CompletedTask;
        }
    }
}

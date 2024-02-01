using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.Extensions.Logging;
using ADP.Portal.Core.Ado.Entities;
using ADP.Portal.Core.Ado.Infrastructure;

namespace ADP.Portal.Core.Ado.Services
{
    public class AdoProjectService : IAdoProjectService
    {
        private readonly ILogger<AdoProjectService> logger;
        private readonly IAdoService adoService;

        public AdoProjectService(IAdoService adoService, ILogger<AdoProjectService> logger)
        {
            this.adoService = adoService;
            this.logger = logger;
        }

        public async Task<TeamProjectReference?> GetProjectAsync(string projectName)
        {
            try
            {
                return await adoService.GetTeamProjectAsync(projectName);
            }
            catch (ProjectDoesNotExistWithNameException)
            {
                logger.LogWarning("Project {projectName} does not exist", projectName);
                return null;
            }
        }

        public async Task OnBoardAsync(string adpProjectName, AdoProject onboardProject)
        {
            await adoService.ShareServiceEndpointsAsync(adpProjectName, onboardProject.ServiceConnections, onboardProject.ProjectReference);

            await adoService.AddEnvironmentsAsync(onboardProject.Environments, onboardProject.ProjectReference);

            await adoService.ShareAgentPoolsAsync(adpProjectName, onboardProject.AgentPools, onboardProject.ProjectReference);

            if(onboardProject.VariableGroups != null)
            {
                await adoService.AddOrUpdateVariableGroupsAsync(onboardProject.VariableGroups, onboardProject.ProjectReference);
            }
        }
    }
}

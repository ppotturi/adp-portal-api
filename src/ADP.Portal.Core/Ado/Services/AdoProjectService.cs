using ADP.Portal.Core.Ado.Dtos;
using ADP.Portal.Core.Ado.Entities;
using ADP.Portal.Core.Ado.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;

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
            catch (ProjectDoesNotExistWithNameException ex)
            {
                logger.LogWarning(ex, "Project {ProjectName} does not exist", projectName);
                return null;
            }
        }

        public async Task<OnboardProjectResult> OnBoardAsync(string adpProjectName, AdoProject onboardProject)
        {
            var onBoardResult = new OnboardProjectResult
            {
                ServiceConnectionIds = await adoService.ShareServiceEndpointsAsync(adpProjectName, onboardProject.ServiceConnections, onboardProject.ProjectReference),
                EnvironmentIds = await adoService.AddEnvironmentsAsync(onboardProject.Environments, onboardProject.ProjectReference),
                AgentQueueIds = await adoService.ShareAgentPoolsAsync(adpProjectName, onboardProject.AgentPools, onboardProject.ProjectReference)
            };

            if (onboardProject.VariableGroups != null)
            {
                onBoardResult.VariableGroupIds = await adoService.AddOrUpdateVariableGroupsAsync(onboardProject.VariableGroups, onboardProject.ProjectReference);
            }

            return onBoardResult;
        }
    }
}
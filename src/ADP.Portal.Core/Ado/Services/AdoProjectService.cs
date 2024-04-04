using ADP.Portal.Core.Ado.Entities;
using ADP.Portal.Core.Ado.Infrastructure;
using ADP.Portal.Core.Ado.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;

namespace ADP.Portal.Core.Ado.Services
{
    public class AdoProjectService : IAdoProjectService
    {
        private readonly ILogger<AdoProjectService> logger;
        private readonly IAdoService adoService;
        private readonly IAdoRestAPIService adoRestAPIService;

        public AdoProjectService(IAdoService adoService, ILogger<AdoProjectService> logger, IAdoRestAPIService adoRestAPIService)
        {
            this.adoRestAPIService = adoRestAPIService;
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
            var onBoardResult = new OnboardProjectResult();

            onBoardResult.ServiceConnectionIds = await adoService.ShareServiceEndpointsAsync(adpProjectName, onboardProject.ServiceConnections, onboardProject.ProjectReference);

            onBoardResult.EnvironmentIds = await adoService.AddEnvironmentsAsync(onboardProject.Environments, onboardProject.ProjectReference);

            onBoardResult.AgentQueueIds = await adoService.ShareAgentPoolsAsync(adpProjectName, onboardProject.AgentPools, onboardProject.ProjectReference);

            if (onboardProject.VariableGroups != null)
            {
                onBoardResult.VariableGroupIds = await adoService.AddOrUpdateVariableGroupsAsync(onboardProject.VariableGroups, onboardProject.ProjectReference);
            }

            string projectAdminUserId = await adoRestAPIService.GetUserIdAsync(adpProjectName, "Project Administrators");
            string contributorsId = await adoRestAPIService.GetUserIdAsync(adpProjectName, "Contributors");
            string projectValidUserId = await adoRestAPIService.GetUserIdAsync(adpProjectName, "Project Valid Users");

            await adoRestAPIService.postRoleAssignmentAsync(onboardProject.ProjectReference.Id.ToString(), onBoardResult.EnvironmentIds.ToList()[0].ToString(), "Administrator",projectAdminUserId);
            await adoRestAPIService.postRoleAssignmentAsync(onboardProject.ProjectReference.Id.ToString(), onBoardResult.EnvironmentIds.ToList()[0].ToString(), "User", contributorsId);
            await adoRestAPIService.postRoleAssignmentAsync(onboardProject.ProjectReference.Id.ToString(), onBoardResult.EnvironmentIds.ToList()[0].ToString(), "Reader", projectValidUserId);

            return onBoardResult;
        }
    }
}
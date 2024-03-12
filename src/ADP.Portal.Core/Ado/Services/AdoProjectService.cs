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
                logger.LogWarning(ex, "Project '{ProjectName}' does not exist", projectName);
                return null;
            }
        }

        public async Task OnBoardAsync(string adpProjectName, AdoProject onboardProject)
        {

            logger.LogInformation("Share service endpoints to the project '{ProjectName}'", onboardProject.ProjectReference.Name);   
            await adoService.ShareServiceEndpointsAsync(adpProjectName, onboardProject.ServiceConnections, onboardProject.ProjectReference);

            logger.LogInformation("Add environments to the project '{ProjectName}'", onboardProject.ProjectReference.Name);
            await adoService.AddEnvironmentsAsync(onboardProject.Environments, onboardProject.ProjectReference);

            logger.LogInformation("Share agent pools to the project '{ProjectName}'", onboardProject.ProjectReference.Name);    
            await adoService.ShareAgentPoolsAsync(adpProjectName, onboardProject.AgentPools, onboardProject.ProjectReference);

            if (onboardProject.VariableGroups != null)
            {
                logger.LogInformation("Add or update variable groups to the project '{ProjectName}'", onboardProject.ProjectReference.Name);
                await adoService.AddOrUpdateVariableGroupsAsync(onboardProject.VariableGroups, onboardProject.ProjectReference);
            }
        }
    }
}

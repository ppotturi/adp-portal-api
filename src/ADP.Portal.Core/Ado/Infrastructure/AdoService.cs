using ADP.Portal.Core.Ado.Entities;
using Mapster;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.ServiceEndpoints;
using Microsoft.VisualStudio.Services.ServiceEndpoints.WebApi;
using DistributedTaskProjectReference = Microsoft.TeamFoundation.DistributedTask.WebApi.ProjectReference;
using ProjectReference = Microsoft.VisualStudio.Services.ServiceEndpoints.WebApi.ProjectReference;

namespace ADP.Portal.Core.Ado.Infrastructure
{
    public class AdoService : IAdoService
    {
        private readonly ILogger<AdoService> logger;
        private readonly IVssConnection vssConnection;

        public AdoService(ILogger<AdoService> logger, Task<IVssConnection> vssConnection)
        {
            this.logger = logger;
            this.vssConnection = vssConnection.Result;
        }

        public async Task<TeamProject> GetTeamProjectAsync(string projectName)
        {
            logger.LogInformation("Getting project {ProjectName}", projectName);
            using var projectClient = await vssConnection.GetClientAsync<ProjectHttpClient>();

            var project = await projectClient.GetProject(projectName);
            return project;
        }

        public async Task ShareServiceEndpointsAsync(string adpProjectName, List<string> serviceConnections, TeamProjectReference onBoardProject)
        {
            var serviceEndpointClient = await vssConnection.GetClientAsync<ServiceEndpointHttpClient>();

            logger.LogInformation("Getting service endpoints for project {AdpProjectName}", adpProjectName);

            var endpoints = await serviceEndpointClient.GetServiceEndpointsAsync(adpProjectName);

            foreach (var serviceConnection in serviceConnections)
            {
                var endpoint = endpoints.Find(e => e.Name.Equals(serviceConnection, StringComparison.OrdinalIgnoreCase));

                if (endpoint != null)
                {
                    var isAlreadyShared = endpoint.ServiceEndpointProjectReferences.Any(r => r.ProjectReference.Id == onBoardProject.Id);
                    if (!isAlreadyShared)
                    {
                        logger.LogInformation("Sharing service endpoint {ServiceConnection} with project {Name}", serviceConnection, onBoardProject.Name);

                        var serviceEndpointProjectReferences = new List<ServiceEndpointProjectReference>() {
                            new() { Name = serviceConnection,ProjectReference = onBoardProject.Adapt<ProjectReference>() }
                        };

                        await serviceEndpointClient.ShareServiceEndpointAsync(endpoint.Id, serviceEndpointProjectReferences);
                    }
                    else
                    {
                        logger.LogInformation("Service endpoint {ServiceConnection} already shared with project {Name}", serviceConnection, onBoardProject.Name);
                    }
                }
                else
                {
                    logger.LogWarning("Service endpoint {ServiceConnection} not found", serviceConnection);
                }
            }
        }

        public async Task AddEnvironmentsAsync(List<AdoEnvironment> adoEnvironments, TeamProjectReference onBoardProject)
        {
            var taskAgentClient = await vssConnection.GetClientAsync<TaskAgentHttpClient>();

            logger.LogInformation("Getting environments for project {Name}", onBoardProject.Name);

            var environments = await taskAgentClient.GetEnvironmentsAsync(onBoardProject.Id);

            foreach (var environment in adoEnvironments)
            {
                var IsEnvironmentExists = environments.Exists(e => e.Name.Equals(environment.Name, StringComparison.OrdinalIgnoreCase));

                if (IsEnvironmentExists)
                {
                    logger.LogInformation("Environment {Name} already exists", environment.Name);
                    continue;
                }

                logger.LogInformation("Creating environment {Name}", environment.Name);

                var environmentParameter = environment.Adapt<EnvironmentCreateParameter>();

                await taskAgentClient.AddEnvironmentAsync(onBoardProject.Id, environmentParameter);

                logger.LogInformation("Environment {Name} created", environment.Name);
            }
        }

        public async Task ShareAgentPoolsAsync(string adpPrjectName, List<string> adoAgentPoolsToShare, TeamProjectReference onBoardProject)
        {
            var taskAgentClient = await vssConnection.GetClientAsync<TaskAgentHttpClient>();

            logger.LogInformation("Getting agent pools for project {Name}", onBoardProject.Name);

            var adpAgentQueues = await taskAgentClient.GetAgentQueuesAsync(adpPrjectName, string.Empty);

            var agentPools = await taskAgentClient.GetAgentQueuesAsync(onBoardProject.Id);

            foreach (var agentPool in adoAgentPoolsToShare)
            {
                var adpAgentQueue = adpAgentQueues.Find(a => a.Name.Equals(agentPool, StringComparison.OrdinalIgnoreCase));
                if (adpAgentQueue != null)
                {
                    var IsAgentPoolExists = agentPools.Exists(e => e.Name.Equals(agentPool, StringComparison.OrdinalIgnoreCase));

                    if (IsAgentPoolExists)
                    {
                        logger.LogInformation("Agent pool {AgentPool} already exists in the {Name} project", agentPool, onBoardProject.Name);
                        continue;
                    }

                    logger.LogInformation("Adding agent pool {AgentPool} to the {Name} project", agentPool, onBoardProject.Name);

                    await taskAgentClient.AddAgentQueueAsync(onBoardProject.Id, adpAgentQueue);

                    logger.LogInformation("Agent pool {AgentPool} created", agentPool);
                }
                else
                {
                    logger.LogWarning("Agent pool {AgentPool} not found in the adp project.", agentPool);
                }
            }
        }

        public async Task AddOrUpdateVariableGroupsAsync(List<AdoVariableGroup> adoVariableGroups, TeamProjectReference onBoardProject)
        {
            var taskAgentClient = await vssConnection.GetClientAsync<TaskAgentHttpClient>();

            logger.LogInformation("Getting variable groups for project {Name}", onBoardProject.Name);

            var variableGroups = await taskAgentClient.GetVariableGroupsAsync(onBoardProject.Id);

            foreach (var variableGroup in adoVariableGroups)
            {
                var existingVariableGroup = variableGroups.Find(e => e.Name.Equals(variableGroup.Name, StringComparison.OrdinalIgnoreCase));

                var variableGroupParameters = variableGroup.Adapt<VariableGroupParameters>();
                variableGroupParameters.VariableGroupProjectReferences[0].ProjectReference = onBoardProject.Adapt<DistributedTaskProjectReference>();

                if (existingVariableGroup == null)
                {
                    logger.LogInformation("Creating variable group {Name}", variableGroup.Name);
                    await taskAgentClient.AddVariableGroupAsync(variableGroupParameters);
                }
                else
                {
                    logger.LogInformation("Updating variable group {Name}", variableGroup.Name);
                    await taskAgentClient.UpdateVariableGroupAsync(existingVariableGroup.Id, variableGroupParameters);
                }
            }
        }
    }
}

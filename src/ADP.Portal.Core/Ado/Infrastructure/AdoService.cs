using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.ServiceEndpoints.WebApi;
using Microsoft.VisualStudio.Services.ServiceEndpoints;
using ADP.Portal.Core.Ado.Entities;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using ProjectReference = Microsoft.VisualStudio.Services.ServiceEndpoints.WebApi.ProjectReference;
using DistributedTaskProjectReference = Microsoft.TeamFoundation.DistributedTask.WebApi.ProjectReference;
using Mapster;

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
            logger.LogInformation("Getting project {projectName}", projectName);
            using var projectClient = await vssConnection.GetClientAsync<ProjectHttpClient>();

            var project = await projectClient.GetProject(projectName);
            return project;
        }

        public async Task<IEnumerable<Guid>> ShareServiceEndpointsAsync(string adpProjectName, List<string> serviceConnections, TeamProjectReference onBoardProject)
        {
            var serviceEndpointClient = await vssConnection.GetClientAsync<ServiceEndpointHttpClient>();

            logger.LogInformation("Getting service endpoints for project {adpProjectName}", adpProjectName);

            var endpoints = await serviceEndpointClient.GetServiceEndpointsAsync(adpProjectName);
            var serviceEndpointIds = new List<Guid>();

            foreach (var serviceConnection in serviceConnections)
            {
                var endpoint = endpoints.Find(e => e.Name.Equals(serviceConnection, StringComparison.OrdinalIgnoreCase));

                if (endpoint != null)
                {
                    var isAlreadyShared = endpoint.ServiceEndpointProjectReferences.Any(r => r.ProjectReference.Id == onBoardProject.Id);
                    if (!isAlreadyShared)
                    {
                        logger.LogInformation("Sharing service endpoint {serviceConnection} with project {Name}", serviceConnection, onBoardProject.Name);

                        var serviceEndpointProjectReferences = new List<ServiceEndpointProjectReference>() {
                            new() { Name = serviceConnection,ProjectReference = onBoardProject.Adapt<ProjectReference>() }
                        };

                        await serviceEndpointClient.ShareServiceEndpointAsync(endpoint.Id, serviceEndpointProjectReferences);
                    }
                    else
                    {
                        logger.LogInformation("Service endpoint {serviceConnection} already shared with project {Name}", serviceConnection, onBoardProject.Name);
                    }

                    serviceEndpointIds.Add(endpoint.Id);
                }
                else
                {
                    logger.LogWarning("Service endpoint {serviceConnection} not found", serviceConnection);
                }
            }

            return serviceEndpointIds;
        }

        public async Task<IEnumerable<int>> AddEnvironmentsAsync(List<AdoEnvironment> adoEnvironments, TeamProjectReference onBoardProject)
        {
            var taskAgentClient = await vssConnection.GetClientAsync<TaskAgentHttpClient>();

            logger.LogInformation("Getting environments for project {Name}", onBoardProject.Name);

            var environments = await taskAgentClient.GetEnvironmentsAsync(onBoardProject.Id);
            var environmentIds = new List<int>();

            foreach (var environment in adoEnvironments)
            {
                var existingEnvironment = environments.SingleOrDefault(e => e.Name.Equals(environment.Name, StringComparison.OrdinalIgnoreCase));
                
                if (existingEnvironment != null)
                {
                    environmentIds.Add(existingEnvironment.Id);
                    logger.LogInformation("Environment {Name} already exists", environment.Name);
                    continue;
                }

                logger.LogInformation("Creating environment {Name}", environment.Name);

                var environmentParameter = environment.Adapt<EnvironmentCreateParameter>();

                var createdEnvironment = await taskAgentClient.AddEnvironmentAsync(onBoardProject.Id, environmentParameter);
                environmentIds.Add(createdEnvironment.Id);

                logger.LogInformation("Environment {Name} created", environment.Name);
            }

            return environmentIds;
        }

        public async Task<IEnumerable<int>> ShareAgentPoolsAsync(string adpProjectName, List<string> adoAgentPoolsToShare, TeamProjectReference onBoardProject)
        {
            var taskAgentClient = await vssConnection.GetClientAsync<TaskAgentHttpClient>();

            logger.LogInformation("Getting agent pools for project {Name}", onBoardProject.Name);

            var adpAgentQueues = await taskAgentClient.GetAgentQueuesAsync(adpProjectName, string.Empty);

            var agentPools = await taskAgentClient.GetAgentQueuesAsync(onBoardProject.Id);
            var agentQueueIds = new List<int>();

            foreach (var agentPool in adoAgentPoolsToShare)
            {
                var adpAgentQueue = adpAgentQueues.Find(a => a.Name.Equals(agentPool, StringComparison.OrdinalIgnoreCase));
                if (adpAgentQueue != null)
                {
                    var existingAgentQueue = agentPools.SingleOrDefault(e => e.Name.Equals(agentPool, StringComparison.OrdinalIgnoreCase));

                    if (existingAgentQueue != null)
                    {
                        logger.LogInformation("Agent pool {agentPool} already exists in the {Name} project", agentPool, onBoardProject.Name);
                        agentQueueIds.Add(existingAgentQueue.Id);
                        continue;
                    }

                    logger.LogInformation("Adding agent pool {agentPool} to the {Name} project", agentPool, onBoardProject.Name);

                    var agentQueue = await taskAgentClient.AddAgentQueueAsync(onBoardProject.Id, adpAgentQueue);
                    agentQueueIds.Add(agentQueue.Id);

                    logger.LogInformation("Agent pool {agentPool} created",agentPool);
                }
                else
                {
                    logger.LogWarning("Agent pool {agentPool} not found in the adp project.", agentPool);
                }
            }

            return agentQueueIds;
        }

        public async Task<IEnumerable<int>> AddOrUpdateVariableGroupsAsync(List<AdoVariableGroup> adoVariableGroups, TeamProjectReference onBoardProject)
        {
            var taskAgentClient = await vssConnection.GetClientAsync<TaskAgentHttpClient>();

            logger.LogInformation("Getting variable groups for project {Name}", onBoardProject.Name);

            var variableGroups = await taskAgentClient.GetVariableGroupsAsync(onBoardProject.Id);
            var variableGroupIds = new List<int>();

            foreach (var variableGroup in adoVariableGroups)
            {
                var existingVariableGroup = variableGroups.Find(e => e.Name.Equals(variableGroup.Name, StringComparison.OrdinalIgnoreCase));

                var variableGroupParameters = variableGroup.Adapt<VariableGroupParameters>();
                variableGroupParameters.VariableGroupProjectReferences[0].ProjectReference = onBoardProject.Adapt<DistributedTaskProjectReference>();

                if (existingVariableGroup == null)
                {
                    logger.LogInformation("Creating variable group {Name}", variableGroup.Name);
                    var newVariableGroup = await taskAgentClient.AddVariableGroupAsync(variableGroupParameters);
                    variableGroupIds.Add(newVariableGroup.Id);
                }
                else
                {
                    logger.LogInformation("Updating variable group {Name}", variableGroup.Name);
                    await taskAgentClient.UpdateVariableGroupAsync(existingVariableGroup.Id, variableGroupParameters);
                    variableGroupIds.Add(existingVariableGroup.Id);
                }
            }

            return variableGroupIds;
        }
    }
}

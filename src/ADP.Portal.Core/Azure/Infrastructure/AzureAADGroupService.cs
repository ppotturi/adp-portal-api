using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace ADP.Portal.Core.Azure.Infrastructure
{
    public class AzureAadGroupService : IAzureAadGroupService
    {
        private readonly GraphServiceClient graphServiceClient;

        public AzureAadGroupService(GraphServiceClient graphServiceClient)
        {
            this.graphServiceClient = graphServiceClient;
        }


        public async Task<string?> GetUserIdAsync(string userPrincipalName)
        {

            var user = await graphServiceClient.Users[userPrincipalName].GetAsync((requestConfiguration) =>
            {
                requestConfiguration.QueryParameters.Select = ["Id"];
            });

            return user?.Id;
        }

        public async Task<bool> ExistingMemberAsync(Guid groupId, string userPrincipalName)
        {
            var existingMember = await graphServiceClient.Groups[groupId.ToString()].Members.GraphUser.GetAsync((requestConfiguration) =>
                   {
                       requestConfiguration.QueryParameters.Count = true;
                       requestConfiguration.QueryParameters.Filter = $"userPrincipalName eq '{userPrincipalName}'";
                       requestConfiguration.Headers.Add("ConsistencyLevel", "eventual");
                   });

            if (existingMember?.Value != null && existingMember.Value.Count == 0)
            {
                return false;
            }

            return true;
        }


        public async Task<bool> AddToAADGroupAsync(Guid groupId, string userId)
        {
            var requestBody = new ReferenceCreate
            {
                OdataId = $"https://graph.microsoft.com/beta/directoryObjects/{userId}",
            };

            await graphServiceClient.Groups[groupId.ToString()].Members.Ref.PostAsync(requestBody);

            return true;
        }
    }
}

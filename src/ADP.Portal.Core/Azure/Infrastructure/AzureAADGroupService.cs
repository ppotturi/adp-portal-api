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
            var user = await graphServiceClient.Users[userPrincipalName].GetAsync((request) =>
            {
                request.QueryParameters.Select = ["Id"];
            });

            return user?.Id;
        }

        public async Task<bool> ExistingMemberAsync(string groupId, string userPrincipalName)
        {
            var existingMember = await graphServiceClient.Groups[groupId.ToString()].Members.GraphUser.GetAsync((request) =>
                   {
                       request.QueryParameters.Count = true;
                       request.QueryParameters.Filter = $"userPrincipalName eq '{userPrincipalName}'";
                       request.Headers.Add("ConsistencyLevel", "eventual");
                   });

            if (existingMember?.Value != null && existingMember.Value.Count == 0)
            {
                return false;
            }

            return true;
        }

        public async Task<bool> AddGroupMemberAsync(string groupId, string directoryObjectId)
        {
            var requestBody = new ReferenceCreate
            {
                OdataId = $"https://graph.microsoft.com/beta/directoryObjects/{directoryObjectId}",
            };

            await graphServiceClient.Groups[groupId.ToString()].Members.Ref.PostAsync(requestBody);

            return true;
        }

        public async Task<bool> RemoveGroupMemberAsync(string groupId, string directoryObjectId)
        {
            await graphServiceClient.Groups[groupId.ToString()].Members[directoryObjectId].Ref.DeleteAsync();
            return true;
        }

        public async Task<string?> GetGroupIdAsync(string groupName)
        {

            var existingGroup = await graphServiceClient.Groups.GetAsync((request) =>
             {
                 request.QueryParameters.Select = ["Id"];
                 request.QueryParameters.Filter = $"displayName eq '{groupName}'";
             });

            if (existingGroup?.Value != null && existingGroup.Value.Count > 0)
            {
                return existingGroup.Value[0].Id;
            }

            return default;
        }

        public async Task<List<T>?> GetGroupMembersAsync<T>(string groupId)
        {
            var result = await graphServiceClient.Groups[groupId].Members.GetAsync((request) =>
            {
                request.QueryParameters.Select = ["id", "userPrincipalName", "displayName"];
            });

            if (result != null)
            {
                return result.Value?.Where(item => item.GetType() == typeof(T)).Select(item => (T)Convert.ChangeType(item, typeof(T))).ToList();
            }
            return default;
        }

        public async Task<List<Group>?> GetGroupMemberShipsAsync(string groupId)
        {
            var result = await graphServiceClient.Groups[groupId].MemberOf.GetAsync((request) =>
            {
                request.QueryParameters.Select = ["id", "displayName"];
            });


            if (result != null)
            {
                return result.Value?.Select(item => (Group)item).ToList();
            }
            return default;
        }

        public async Task<Group?> AddGroupAsync(Group group)
        {
            var result = await graphServiceClient.Groups.PostAsync(group);

            return result;
        }

    }
}

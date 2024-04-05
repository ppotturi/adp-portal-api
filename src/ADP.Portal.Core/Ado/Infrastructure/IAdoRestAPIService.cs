namespace ADP.Portal.Core.Ado.Infrastructure
{
    public interface IAdoRestApiService
    {
        Task<String?> GetUserIdAsync(string projectName, string userName);

        Task<bool> postRoleAssignmentAsync(string projectId, string envId, string roleName, string userId);
    }
}

namespace ADP.Portal.Core.Ado.Infrastructure
{
    public interface IAdoRestApiService
    {
        Task<String> GetUserIdAsync(string projectName, string userName);

        Task<bool> updateRoleAssignmentAsync(string projectId, string envId);
    }
}

using ADP.Portal.Api.Models.Ado;

namespace ADP.Portal.Core.Ado.Infrastructure
{
    public interface IAdoRestApiService
    {
        Task<List<AdoSecurityRole>> GetRoleAssignmentAsync(string projectId, string envId);

        Task<bool> updateRoleAssignmentAsync(string projectId, string envId, List<AdoSecurityRole> adoSecurityRoleList);
    }
}

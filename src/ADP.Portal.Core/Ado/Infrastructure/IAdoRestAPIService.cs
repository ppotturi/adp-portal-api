using Microsoft.TeamFoundation.Core.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADP.Portal.Core.Ado.Infrastructure
{
    public interface IAdoRestAPIService
    {
        Task<String> GetUserIdAsync(string projectName, string userName);

        Task postRoleAssignmentAsync(string projectId, string envId, string roleName, string userId);
    }
}

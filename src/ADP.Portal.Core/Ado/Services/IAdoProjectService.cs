using ADP.Portal.Core.Ado.Dtos;
using ADP.Portal.Core.Ado.Entities;
using Microsoft.TeamFoundation.Core.WebApi;

namespace ADP.Portal.Core.Ado.Services
{
    public interface IAdoProjectService
    {
        public Task<TeamProjectReference?> GetProjectAsync(string projectName);

        public Task<OnboardProjectResult> OnBoardAsync(string adpProjectName, AdoProject onboardProject);
    }
}

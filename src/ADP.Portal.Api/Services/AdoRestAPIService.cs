using ADP.Portal.Api.Config;
using ADP.Portal.Api.Models.Ado;
using ADP.Portal.Core.Ado.Infrastructure;
using ADP.Portal.Core.Helpers;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace ADP.Portal.Api.Services
{
    public class AdoRestApiService : IAdoRestApiService
    {
        private readonly ILogger<AdoRestApiService> logger;
        private readonly string adoOrgUrl;
        private readonly HttpClient client;

        public AdoRestApiService(ILogger<AdoRestApiService> logger, IOptions<AdoConfig> configuration)
        {
            this.logger = logger;
            adoOrgUrl = configuration.Value.OrganizationUrl;
            client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(
                    System.Text.Encoding.ASCII.GetBytes(
                        string.Format("{0}:{1}", "", configuration.Value.PatToken))));
        }



        public async Task<string> GetUserIdAsync(string projectName, string userName)
        {
            var uri = adoOrgUrl.Replace("dev.azure.com", "vssps.dev.azure.com") + "/_apis/identities?searchFilter=General&filterValue=[" + projectName + "]\\" + userName + "&queryMembership=None&api-version=7.1-preview.1";
            try
            {
                var response = await client.GetFromJsonAsync<JsonAdoGroupWrapper>(uri);
                return response?.value?.FirstOrDefault()?.id ?? "";
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Exception {Message}", ex.Message);
            }
            return "";
        }

        public async Task<bool> postRoleAssignmentAsync(string projectId, string envId, string roleName, string userId)
        {
            var uri = adoOrgUrl + "/_apis/securityroles/scopes/distributedtask.environmentreferencerole/roleassignments/resources/" + projectId + "_" + envId + "?api-version=7.1-preview.1";
            List<AdoSecurityRole> adoSecurityRoleList = new();
            adoSecurityRoleList.Add(new AdoSecurityRole { roleName = roleName, userId = userId });

            var postRequest = new HttpRequestMessage(HttpMethod.Put, uri)
            {
                Content = JsonContent.Create(adoSecurityRoleList)
            };
            var response = await client.SendAsync(postRequest);

            return response.IsSuccessStatusCode;
        }
    }
}

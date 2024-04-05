using ADP.Portal.Api.Config;
using ADP.Portal.Api.Models.Ado;
using ADP.Portal.Core.Ado.Infrastructure;
using ADP.Portal.Core.Helpers;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace ADP.Portal.Api.Services
{
    public class AdoRestAPIService : IAdoRestAPIService
    {
        private readonly ILogger<AdoRestAPIService> logger;
        private readonly IOptions<AdoConfig> configuration;
        private readonly string adoOrgUrl;
        private readonly string adoPatToken;
        private readonly HttpClient client;

        public AdoRestAPIService(ILogger<AdoRestAPIService> logger, IOptions<AdoConfig> configuration)
        {
            this.logger = logger;
            this.configuration = configuration;
            adoOrgUrl = configuration.Value.OrganizationUrl;
            adoPatToken = configuration.Value.PatToken is null ? "" : configuration.Value.PatToken;
            client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(
                    System.Text.Encoding.ASCII.GetBytes(
                        string.Format("{0}:{1}", "", adoPatToken))));
        }



        public async Task<string> GetUserIdAsync(string projectName, string userName)
        {
            string userId = "";
            try
            {
                var newOrgUrl = new StringHelper().ReplaceFirst(adoOrgUrl, "dev.azure.com", "vssps.dev.azure.com");
                var uri = newOrgUrl + "/_apis/identities?searchFilter=General&filterValue=[" + projectName + "]\\" + userName + "&queryMembership=None&api-version=7.1-preview.1";
                var response = await client.GetFromJsonAsync<JsonAdoGroupWrapper>(uri);
                if (response != null && response.value != null)
                {
                    userId = response.value[0].id;
                    logger.LogInformation(" '{userId}' .", userId);
                }
                else
                {
                    logger.LogWarning(" '{userId} not found' .", userId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }

            return userId;
        }

        public async Task<bool> postRoleAssignmentAsync(string projectId, string envId, string roleName, string userId)
        {
            var uri = adoOrgUrl + "/_apis/securityroles/scopes/distributedtask.environmentreferencerole/roleassignments/resources/" + projectId + "_" + envId + "?api-version=7.1-preview.1";
            var postsecurityRole = new AdoSecurityRole { roleName = roleName, userId = userId };
            List<AdoSecurityRole> adoSecurityRoleList = new List<AdoSecurityRole>();
            adoSecurityRoleList.Add(postsecurityRole);

            var postRequest = new HttpRequestMessage(HttpMethod.Put, uri)
            {
                Content = JsonContent.Create(adoSecurityRoleList)
            };
            try
            {
                var postResponse = await client.SendAsync(postRequest);
                logger.LogInformation("Role {roleName} assigned to {userId} ", roleName, userId);
                postResponse.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }
            return true;
        }
    }
}

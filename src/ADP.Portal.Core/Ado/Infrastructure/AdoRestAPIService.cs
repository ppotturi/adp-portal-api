using ADP.Portal.Api.Models.Ado;
using ADP.Portal.Core.Ado.Client;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;


namespace ADP.Portal.Core.Ado.Infrastructure
{

    public class AdoRestApiService : IAdoRestApiService
    {
        private readonly ILogger<AdoRestApiService> logger;
        private readonly string adoOrgUrl;
        private HttpClient client;

        public AdoRestApiService(ILogger<AdoRestApiService> logger, Task<IVssConnection> vssConnection)
        {
            this.logger = logger;
            ADORestHttpClient adoRestHttpClient = vssConnection.Result.GetClient<ADORestHttpClient>();
            this.client = adoRestHttpClient.getHttpClient();
            this.adoOrgUrl = adoRestHttpClient.getOrganizationUrl();
        }

        public AdoRestApiService(ILogger<AdoRestApiService> logger, string organizationUrl, HttpClient client)
        {
            this.logger = logger;
            this.client = client;
            this.adoOrgUrl = organizationUrl;
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

        public async Task<bool> updateRoleAssignmentAsync(string projectId, string envId)
        {
            var uri = adoOrgUrl + "/_apis/securityroles/scopes/distributedtask.environmentreferencerole/roleassignments/resources/" + projectId + "_" + envId + "?api-version=7.1-preview.1";
            List<AdoSecurityRole> adoSecurityRoleList = new();

            var roleDetails = await client.GetFromJsonAsync<JsonAdoSecurityRoleWrapper>(uri);
            if (roleDetails != null && roleDetails.count > 0 && roleDetails.value != null)
            {
                foreach (var roleObj in roleDetails.value)
                {
                    var identityName = roleObj.identity.displayName.Split('\\').Last();
                    var id = roleObj.identity.id;
                    switch (identityName)
                    {
                        case "Project Administrators":
                            adoSecurityRoleList.Add(new AdoSecurityRole { roleName = "Administrator", userId = id });
                            break;
                        case "Project Valid Users":
                            adoSecurityRoleList.Add(new AdoSecurityRole { roleName = "Reader", userId = id });
                            break;
                        case "Contributors":
                            adoSecurityRoleList.Add(new AdoSecurityRole { roleName = "User", userId = id });
                            break;
                        default:
                            break;
                    }
                }
            }

            var postRequest = new HttpRequestMessage(HttpMethod.Put, uri)
            {
                Content = JsonContent.Create(adoSecurityRoleList)
            };

            var response = await client.SendAsync(postRequest);

            return response.IsSuccessStatusCode;
        }
    }
}

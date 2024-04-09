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
        private readonly HttpClient client;

        public AdoRestApiService(ILogger<AdoRestApiService> logger, Task<IVssConnection> vssConnection)
        {
            this.logger = logger;
            AdoRestHttpClient adoRestHttpClient = vssConnection.Result.GetClient<AdoRestHttpClient>();

            this.client = adoRestHttpClient?.getHttpClient() ?? new HttpClient();
            this.adoOrgUrl = adoRestHttpClient?.getOrganizationUrl() ?? "";
        }

        public AdoRestApiService(ILogger<AdoRestApiService> logger, string organizationUrl, HttpClient client)
        {
            this.logger = logger;
            this.client = client;
            this.adoOrgUrl = organizationUrl;
        }



        public async Task<List<AdoSecurityRole>> GetRoleAssignmentAsync(string projectId, string envId)
        {

            var uri = adoOrgUrl + "/_apis/securityroles/scopes/distributedtask.environmentreferencerole/roleassignments/resources/" + projectId + "_" + envId + "?api-version=7.1-preview.1";
            List<AdoSecurityRole> adoSecurityRoleList = new();

            var roleDetails = await client.GetFromJsonAsync<JsonAdoSecurityRoleWrapper>(uri);
            if (roleDetails != null && roleDetails.count > 0 && roleDetails.value != null)
            {
                foreach (var identity in roleDetails.value.Select(roleObj => roleObj.identity))
                {
                    var displayName = identity.displayName.Split('\\');
                    var identityName = identity.displayName.Split('\\')[displayName.Length - 1];
                    var id = identity.id;
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
            logger.LogInformation("Security Role List: {SecurityRoleList} ", adoSecurityRoleList.ToString());
            return adoSecurityRoleList;
        }

        public async Task<bool> updateRoleAssignmentAsync(string projectId, string envId, List<AdoSecurityRole> adoSecurityRoleList)
        {

            var uri = adoOrgUrl + "/_apis/securityroles/scopes/distributedtask.environmentreferencerole/roleassignments/resources/" + projectId + "_" + envId + "?api-version=7.1-preview.1";
            var postRequest = new HttpRequestMessage(HttpMethod.Put, uri)
            {
                Content = JsonContent.Create(adoSecurityRoleList)
            };

            var response = await client.SendAsync(postRequest);
            logger.LogInformation("Security Role Assignment Updated");
            return response.IsSuccessStatusCode;
        }
    }
}

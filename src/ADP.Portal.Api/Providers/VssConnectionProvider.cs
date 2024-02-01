using ADP.Portal.Api.Config;
using ADP.Portal.Api.Wrappers;
using ADP.Portal.Core.Ado.Infrastructure;
using Azure.Identity;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.OAuth;

namespace ADP.Portal.Api.Providers
{
    public class VssConnectionProvider
    {
        private readonly AdoConfig adoConfig;
        private readonly IAzureCredential azureCredential;
        private const string azureDevOpsScope = "https://app.vssps.visualstudio.com/.default";

        public VssConnectionProvider(IAzureCredential azureCredential, AdoConfig adoConfig)
        {
            this.azureCredential = azureCredential;
            this.adoConfig = adoConfig;
        }

        public async Task<IVssConnection> GetConnectionAsync()
        {
            IVssConnection connection;

            if (adoConfig.UsePatToken)
            {
                var patToken =  adoConfig.PatToken;
                connection = new VssConnectionWrapper(new Uri(adoConfig.OrganizationUrl), new VssBasicCredential(string.Empty, patToken));
            }
            else
            {
                var accessToken = await GetAccessTokenAsync(azureDevOpsScope);
                connection = new VssConnectionWrapper(new Uri(adoConfig.OrganizationUrl), new VssOAuthAccessTokenCredential(accessToken));
            }

            return connection;
        }

        private async Task<string> GetAccessTokenAsync(string azureDevOpsScope)
        {
            var token = await azureCredential.GetTokenAsync(new Azure.Core.TokenRequestContext(new[] { azureDevOpsScope }));
            return token.Token;
        }
    }
}

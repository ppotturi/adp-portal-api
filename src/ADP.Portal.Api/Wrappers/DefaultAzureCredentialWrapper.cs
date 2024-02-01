using Azure.Core;
using Azure.Identity;

namespace ADP.Portal.Api.Wrappers
{
    public class DefaultAzureCredentialWrapper : IAzureCredential
    {
        private readonly DefaultAzureCredential defaultAzureCredential;

        public DefaultAzureCredentialWrapper(DefaultAzureCredentialOptions? options = default)
        {
            defaultAzureCredential = new DefaultAzureCredential(options);
        }
        public async Task<AccessToken> GetTokenAsync(TokenRequestContext requestContext)
        {
            return await defaultAzureCredential.GetTokenAsync(requestContext);
        }
    }
}

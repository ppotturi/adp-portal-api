using Azure.Core;

namespace ADP.Portal.Api.Wrappers
{
    public interface IAzureCredential
    {
        Task<AccessToken> GetTokenAsync(TokenRequestContext requestContext);
    }
}

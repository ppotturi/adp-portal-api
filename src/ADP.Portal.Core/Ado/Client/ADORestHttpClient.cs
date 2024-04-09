using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace ADP.Portal.Core.Ado.Client
{
    public class AdoRestHttpClient : VssHttpClientBase
    {
        private readonly Uri baseUrl;

        public AdoRestHttpClient(Uri baseUrl, VssCredentials credentials) : base(baseUrl, credentials)
        {
            this.baseUrl = baseUrl;
        }

        public AdoRestHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings) : base(baseUrl, credentials, settings)
        {
            this.baseUrl = baseUrl;
        }

        public AdoRestHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers) : base(baseUrl, credentials, handlers)

        {
            this.baseUrl = baseUrl;
        }

        public AdoRestHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler) : base(baseUrl, pipeline, disposeHandler)

        {
            this.baseUrl = baseUrl;
        }

        public AdoRestHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers) : base(baseUrl, credentials, settings, handlers)

        {
            this.baseUrl = baseUrl;
        }

        public HttpClient getHttpClient()
        {
            return base.Client;
        }

        public string getOrganizationUrl()
        {
            return this.baseUrl.ToString();
        }
    }
}

using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.Graph.Constants;

namespace ADP.Portal.Core.Ado.Client
{
    public class ADORestHttpClient : VssHttpClientBase
    {
        private Uri baseUrl;
        private VssHttpRequestSettings? settings;

        public ADORestHttpClient(Uri baseUrl, VssCredentials credentials) : base(baseUrl, credentials)
        {
            this.baseUrl = baseUrl;
        }

        public ADORestHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings) : base(baseUrl, credentials, settings)
        {
            this.baseUrl = baseUrl;
            this.settings = settings;
        }

        public ADORestHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers) : base(baseUrl, credentials, handlers)

        {
            this.baseUrl = baseUrl;
        }

        public ADORestHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler) : base(baseUrl, pipeline, disposeHandler)

        {
            this.baseUrl = baseUrl;
        }

        public ADORestHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers) : base(baseUrl, credentials, settings, handlers)

        {
            this.baseUrl = baseUrl;
            this.settings = settings;
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

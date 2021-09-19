using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Common
{
    public class RefitMessageHandler: DelegatingHandler
    {
        private const string _ERROR_ID_HEADER = "error.id";
        private const string _SERVICE_NAME_HEADER = "exception.message";

        
        private readonly ILogger<RefitMessageHandler> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RefitMessageHandler(ILogger<RefitMessageHandler> logger, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                string errorCode = "";
                string serviceName = "";

                
                errorCode = response.Headers?.GetValues(_ERROR_ID_HEADER)?.FirstOrDefault();
                serviceName = response.Headers?.GetValues(_SERVICE_NAME_HEADER)?.FirstOrDefault();
                
                _httpContextAccessor.HttpContext.Response.Headers.Add(_ERROR_ID_HEADER, errorCode);
                _httpContextAccessor.HttpContext.Response.Headers.Add(_SERVICE_NAME_HEADER, serviceName);
                
                _logger.LogError("Refit exception. Error code: {errorCode}. Originated in service: {serviceName}", errorCode, serviceName);
            }
            return response;
        }
        
       
    }
}
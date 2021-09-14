using System;
using System.Diagnostics;
using System.Net.Http;
using Microsoft.Extensions.Options;
using Refit;

namespace Common
{
    public class RefitServiceResolver : IRefitServiceResolver
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ServiceSettings _serviceSettings;


        public RefitServiceResolver(IHttpClientFactory httpClientFactory, IOptions<ServiceSettings> serviceSettings)
        {
            _httpClientFactory = httpClientFactory;
            _serviceSettings = serviceSettings.Value;
        }

        public T GetRefitService<T>(string serviceUrl)
        {
            var refitsettings = new RefitSettings
            {
                JsonSerializerSettings = CustomJsonSerializerSettings.Instance,
            };

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(serviceUrl);

            // Add API Name as http agent header for internal communication tracking
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(GetUserAgent());
    
            // No need for CSRF with (cookieless) Rest API!
            //InjectCSRFSecuitry(httpClient);

            return RestService.For<T>(httpClient, refitsettings);
        }

        private string GetUserAgent()
        {
            var apiName = _serviceSettings.ApiName ?? "";
            var osName = Environment.OSVersion?.VersionString;
            var machineName = Environment.MachineName;
            var tentantId = _serviceSettings.TenantId ?? "";

            return $"{apiName} ({osName}) Tentant/{tentantId} {machineName}";
        }
    }
}
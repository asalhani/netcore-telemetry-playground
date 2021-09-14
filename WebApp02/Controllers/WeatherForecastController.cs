using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Common;
using Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Refit;
using Serilog;

namespace WebApp02.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IWebApp01 _webApp01Api;
        private readonly IHttpClientFactory _clientFactory;
        



        public WeatherForecastController(ILogger<WeatherForecastController> logger, IWebApp01 webApp01Api, IHttpClientFactory clientFactory)
        {
            _logger = logger;
            _webApp01Api = webApp01Api;
            _clientFactory = clientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            Log.Logger.Information("Event from: Log.Logger.Information");
            Log.Information("Event from: Log.Information");
            
            Activity.Current?.AddEvent(new ActivityEvent("Adib event"));

            var result = await _webApp01Api.GetAll();
            Activity.Current?.AddEvent(new ActivityEvent("Adib event"));
            await _webApp01Api.PostRequest(new PostRequestParam() {Name = "test name"}, "this is header value from webapp02 controller");
            return Ok(result);
            
            // var client = _clientFactory.CreateClient();
            // client.BaseAddress = new Uri("https://www.metaweather.com");
            // return Ok(await client.GetAsync("/api/location/565346/"));
            // ActivitySource activitySource = new ActivitySource(
            //     "companyname.product.instrumentationlibrary",
            //     "semver1.0.0");
            // using (var activity = activitySource.StartActivity("ActivityName"))
            // {
            //     var client = _clientFactory.CreateClient();
            //     client.BaseAddress = new Uri("https://www.metaweather.com");
            //     activity?.SetTag("otel.status_code", "ERROR");
            //     activity?.SetTag("otel.status_description", "error status description");
            //     return Ok(await client.GetAsync("/api/location/565346/"));
            // }

            // ActivitySource activitySource = new ActivitySource(
            //     "companyname.product.instrumentationlibrary",
            //     "semver1.0.0");
            // using (var activity = activitySource.StartActivity("ActivityName"))
            // {
            //     var result = await _webApp01Api.GetAll();
            //     return Ok(result);
            // }
        }
    }
}
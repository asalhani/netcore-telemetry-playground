using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace Common
{
    public static class ServiceConfigurationHelper
    {
        private static readonly string CorsEnabledPolicy = "CorsEnabledPolicy";

        public static void AddRefitService<T>(this IServiceCollection services, string serviceUrl) where T : class
        {
            services.AddTransient(p => p.GetService<IRefitServiceResolver>().GetRefitService<T>(serviceUrl));
        }

        public static void AddRefitService<T>(this IServiceCollection services,
            Func<IServiceProvider, string> implementationFactory) where T : class
        {
            services.AddTransient(p =>
                p.GetService<IRefitServiceResolver>().GetRefitService<T>(implementationFactory.Invoke(p)));
        }


        public static IConfiguration BuildConfiguration()
        {
            var currentEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"appsettings.json", false)
                .AddJsonFile($"appsettings.{currentEnv}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
        }

        public static void SetupService<startup>(string[] args) where startup : class
        {
            SetupService<startup>(args, BuildConfiguration());
        }

        public static void SetupService<startup>(string[] args, IConfiguration configuration) where startup : class
        {
            try
            {
                var host = WebHost.CreateDefaultBuilder(args)
                    .UseConfiguration(configuration)
                    .UseStartup<startup>()
                    .UseSerilog()
                    .Build();

                LoggingHelper.SetupLogger(configuration);

                host.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Log.Logger.Error("Startup Setup Error. {@ex}", ex);
            }
        }

        public static void AddTelemetry(this IServiceCollection services, IWebHostEnvironment _currentEnvironment,
            IConfiguration Configuration)
        {
            services.AddSingleton<ServiceSettings>();
            var serviceConfig = Configuration.GetSection("AppSettings").Get<ServiceSettings>();

            services.AddSingleton<TelemetryConfiguration>();
            var telemetryConfiguration =
                Configuration.GetSection("TelemetryConfiguration").Get<TelemetryConfiguration>();

            // if (telemetryConfiguration == null || !telemetryConfiguration.Enabled)
            //     return;

            services.Configure<AspNetCoreInstrumentationOptions>(options =>
            {
                // options.Enrich = (activity, s, arg3) =>
                // {
                //   
                // };
                // options.Filter = (httpContext) =>
                // {
                //     // only collect telemetry about HTTP GET requests
                //     return httpContext.Request.Method.Equals("GET");
                // };
            });

            // using var redisConnection = ConnectionMultiplexer.Connect("localhost:6379");
            services.AddOpenTelemetryTracing(
                builder =>
                {
                    builder
                        // SetResourceBuilder --> add a set of common attributes to all spans created in the application.
                        .SetResourceBuilder(ResourceBuilder
                            .CreateDefault()
                            .AddService
                            (_currentEnvironment.ApplicationName
                                , serviceInstanceId: $@"{serviceConfig.TenantId}-{serviceConfig.ApiName}"
                                , serviceNamespace: "core"
                                , serviceVersion: "1.0.0"))
                        // where we enable collection of attributes relating to ASP.NET Core requests and responses.
                        .AddAspNetCoreInstrumentation()
                        // .AddSqlClientInstrumentation()
                        // .AddRedisInstrumentation(redisConnection)
                        // .AddSource("Sample.DistributedTracing")
                        .AddJaegerExporter(config =>
                        {
                            config.AgentHost = "localhost";
                            config.AgentPort = 6831;
                        });
                    if (_currentEnvironment.IsDevelopment())
                    {
                        // export the trace data to the debug output
                        builder.AddConsoleExporter(options => options.Targets = ConsoleExporterOutputTargets.Debug);
                    }

                    if (telemetryConfiguration?.HttpClientInstrumentationConfiguration?.Enabled == true)
                    {
                        builder.AddHttpClientInstrumentation(options => options.Enrich
                            = (activity, eventName, rawObject) =>
                            {
                                switch (eventName)
                                {
                                    case "OnStartActivity":
                                    {
                                        if (rawObject is HttpRequestMessage request)
                                        {
                                            // capture request headers
                                            if (telemetryConfiguration?
                                                    .HttpClientInstrumentationConfiguration?
                                                    .TagRequestHeadersOption?
                                                    .Enabled
                                                == true)
                                            {
                                                if (request.Headers != null)
                                                {
                                                    var headersToExclude = telemetryConfiguration
                                                        .HttpClientInstrumentationConfiguration
                                                        .TagRequestHeadersOption
                                                        .ExcludedHeaders;

                                                    Dictionary<string, object> headers =
                                                        new Dictionary<string, object>();
                                                    foreach (var header in request.Headers)
                                                    {
                                                        // check if header not mentioned in excluded headers config. list
                                                        if (!headersToExclude.Contains(header.Key,
                                                            StringComparer.OrdinalIgnoreCase))
                                                            headers.Add(header.Key, header.Value);
                                                    }

                                                    activity.SetTag("request.headers",
                                                        JsonConvert.SerializeObject(headers));
                                                }
                                            }


                                            // capture request body if POST
                                            if (telemetryConfiguration?
                                                .HttpClientInstrumentationConfiguration?
                                                .TagRequestBody == true)
                                            {
                                                if (request.Method == HttpMethod.Post)
                                                {
                                                    if (request.Content != null)
                                                    {
                                                        var requestBody = request.Content
                                                            .ReadAsStringAsync().Result;
                                                        activity.SetTag("request.body", requestBody);
                                                    }
                                                }
                                            }
                                        }

                                        break;
                                    }
                                    case "OnStopActivity":
                                    {
                                        if (rawObject is HttpResponseMessage response)
                                        {
                                            // capture request headers
                                            if (telemetryConfiguration?
                                                    .HttpClientInstrumentationConfiguration?
                                                    .TagResponseHeadersOption?
                                                    .Enabled
                                                == true)
                                            {
                                                if (response.Headers != null)
                                                {
                                                    var headersToExclude = telemetryConfiguration
                                                        .HttpClientInstrumentationConfiguration
                                                        .TagResponseHeadersOption
                                                        .ExcludedHeaders;

                                                    Dictionary<string, object> headers =
                                                        new Dictionary<string, object>();
                                                    foreach (var header in response.Headers)
                                                    {
                                                        // check if header not mentioned in excluded headers config. list
                                                        if (!headersToExclude.Contains(header.Key,
                                                            StringComparer.OrdinalIgnoreCase))
                                                            headers.Add(header.Key, header.Value);
                                                    }

                                                    activity.SetTag("response.headers",
                                                        JsonConvert.SerializeObject(headers));
                                                }
                                            }


                                            // capture request body if POST
                                            if (telemetryConfiguration?
                                                .HttpClientInstrumentationConfiguration?
                                                .TagResponseBody == true)
                                            {
                                                if (response.Content != null)
                                                {
                                                    var responseBody = response.Content
                                                        .ReadAsStringAsync().Result;
                                                    activity.SetTag("response.body", responseBody);
                                                }
                                            }
                                        }

                                        break;
                                    }
                                    case "OnException":
                                    {
                                        if (rawObject is Exception exception)
                                        {
                                            activity.SetTag("stackTrace", exception.StackTrace);
                                        }

                                        break;
                                    }
                                }
                            });
                    }
                });
        }

        public static void UseServiceConfiguration(this IApplicationBuilder app
            , IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMiddleware<EnableRequestRewindMiddleware>();
            app.UseMiddleware<ServiceExceptionHandler>();

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthorization();

            app.UseCors(CorsEnabledPolicy);

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MsTestConsoleApp1
{
    class Program
    {
        private static ActivitySource source = new ActivitySource("Sample.DistributedTracing", "1.0.0");
        private static ILogger _logger;
        
        static async  Task Main(string[] args)
        {
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            Activity.ForceDefaultIdFormat = true;

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddOpenTelemetry(options => options
                    .AddConsoleExporter());
            });
            _logger= loggerFactory.CreateLogger<Program>();
            
            using var tracerProvider = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MySample"))
                .AddSource("Sample.DistributedTracing")
                .AddConsoleExporter()
                .AddJaegerExporter(config =>
                {
                    config.AgentHost = "localhost";
                    config.AgentPort = 6831;
                })
                .Build();


            await DoSomeWork("banana", 8);
            _logger.LogWarning("Example work done");
        }
        
        // All the functions below simulate doing some arbitrary work
        static async Task DoSomeWork(string foo, int bar)
        {
            using (Activity activity = source.StartActivity("SomeWork"))
            {
                _logger.LogInformation("SomeWork log !!!!!!");
                await StepOne();
                activity?.AddEvent(new ActivityEvent("Part way there"));
                await StepTwo();
            }
        }

        static async Task StepOne()
        {
            using (Activity activity = source.StartActivity("StepOne"))
            {
                _logger.LogError("StepOne Error !!!!");
                 await Task.Delay(500);
            }
           
        }

        static async Task StepTwo()
        {
            using (Activity activity = source.StartActivity("StepTwo"))
            {
                await Task.Delay(1000);
            }
            
        }
    }
}
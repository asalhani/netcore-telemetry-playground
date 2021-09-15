using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Common
{
     public class ServiceExceptionHandler
    {
        private readonly RequestDelegate _next;

        public ServiceExceptionHandler(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next.Invoke(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var isJsonRequest = context.Request.ContentType != null &&
                context.Request.ContentType.Contains("application/json") ? true : false;

            var response = context.Response;
            int code = (int)HttpStatusCode.BadRequest;
            var message = exception.Message;
            var errorId = Guid.NewGuid();
            var requestBody = "";


            string errorMsg = string.Empty;

            if (isJsonRequest)
            {
                try
                {
                    context.Request.Body.Seek(0, SeekOrigin.Begin);
                    var memoryStream = new MemoryStream();
                    await context.Request.Body.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;
                    requestBody = new StreamReader(memoryStream).ReadToEnd();
                }
                catch (Exception ex) { }

                errorMsg = context.Request.Method == HttpMethods.Post ? "Request Body " + requestBody + " " + exception.Message + "-" + errorId :
                    exception.Message + "-" + errorId;
            }
            else
            {
                errorMsg = exception.Message;
            }

            if ((exception is AggregateException) && ((exception is ServiceAggregateException) == false))
            {
                var aggregateException = exception as AggregateException;
                exception = new ServiceAggregateException(aggregateException.InnerExceptions);
            }
            string result;

            switch (exception)
            {
                case ServiceAggregateException aggregateException:
                    result = new ServiceAggregateExceptionConfiguration(context, aggregateException).Configure();
                    break;

                case Refit.ApiException apiException:
                    result = new RefitApiExceptionConfiguration(context, apiException).Configure();
                    break;

                case ServiceBaseException serviceBaseException:
                    result = new ServiceBaseExceptionConfiguration(context, serviceBaseException).Configure();
                    break;

                default:
                    code = (int)HttpStatusCode.InternalServerError;
                    result = new GeneralExceptionConfiguration(context, exception).Configure();
                    break;

            }

            var currentEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (currentEnv?.ToLower() != "prod")
            {
                var json = JObject.Parse(result);
                json.Add("ExceptionStackTrace", exception.ToString());
                
                result = json.ToString();
            }

            response.StatusCode = (int)code;
            response.ContentType = "application/json";

            // Destructuring the exception object manually will lead to 
            // errors when the nesting level is more than Maximum destructuring depth
            Log.ForContext("Type", "Error")
               .ForContext("Request Url", context.Request.Path.Value, destructureObjects: true)
               .Error(exception, $"{result} {Environment.NewLine} {errorMsg} ", errorId);

            await response.WriteAsync(result);
        }
    }
}
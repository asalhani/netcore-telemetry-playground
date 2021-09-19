using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Common
{
     public class ServiceExceptionHandler
     {
          
         private const string _ERROR_ID_HEADER = "error.id";
         private const string _SERVICE_NAME_HEADER = "exception.message";
        private readonly RequestDelegate _next;
        private readonly ServiceSettings _serviceSettings;

        public ServiceExceptionHandler(RequestDelegate next, IOptions<ServiceSettings> serviceSettings)
        {
            _next = next;
            _serviceSettings = serviceSettings.Value;
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
            
            // check if header already having error.id field
            var errorId = response.Headers.ContainsKey(_ERROR_ID_HEADER) 
                ? new Guid(response.Headers[_ERROR_ID_HEADER])
                : Guid.NewGuid();
            
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
                    result = new ServiceAggregateExceptionConfiguration(context, aggregateException).Configure(errorId);
                    break;

                case Refit.ApiException apiException:
                    result = new RefitApiExceptionConfiguration(context, apiException).Configure(errorId);
                    break;

                case ServiceBaseException serviceBaseException:
                    result = new ServiceBaseExceptionConfiguration(context, serviceBaseException).Configure(errorId);
                    break;

                default:
                    code = (int)HttpStatusCode.InternalServerError;
                    result = new GeneralExceptionConfiguration(context, exception).Configure(errorId);
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
            
            // push errorId and service name in whcih the expection happned to the header
            if(!response.Headers.ContainsKey(_ERROR_ID_HEADER))
                response.Headers.Add(_ERROR_ID_HEADER, errorId.ToString());
            
            if(!response.Headers.ContainsKey(_SERVICE_NAME_HEADER))
                response.Headers.Add(_SERVICE_NAME_HEADER, _serviceSettings.ApiName);

            // Destructuring the exception object manually will lead to 
            // errors when the nesting level is more than Maximum destructuring depth
            Log.ForContext("Type", "Error")
               .ForContext("Request Url", context.Request.Path.Value, destructureObjects: true)
               .Error(exception, $"{result} {Environment.NewLine} {errorMsg} ", errorId);

            await response.WriteAsync(result);
        }

        private string EncodeException(Exception exception)
        {
            return WebUtility.UrlEncode(exception.ToString());
        }
    }
}
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Refit;
using Serilog;

namespace Common
{
    public class ApiErrorResultOutput
    {
        public ApiErrorResult Error { get; set; }
    }
    
   public class ServiceBaseExceptionConfiguration : IExceptionConfiguration
    {
        private readonly Exception _exception;
        private readonly HttpContext _context;
        public ServiceBaseExceptionConfiguration(HttpContext context, Exception exception)
        {
            _context = context;
            _exception = exception;
        }

        public string Configure()
        {
            var message = _exception.Message;
            var errorId = Guid.NewGuid();

            Log.ForContext("Type", "Error")
                .ForContext("Exception", _exception, destructureObjects: true)
                .ForContext("Request Url", _context.Request.Path.Value, destructureObjects: true)
                .Error(_exception, message + "-" + errorId, errorId);


            int.TryParse(_exception.GetType().GetProperty("ErrorCode")?.GetValue(_exception, null).ToString(), out int errorCode);

            //Return List<Errors>
            var error = new ApiErrorListResultOutput()
            {
                Errors = new List<ApiErrorResult>()
                {
                    //SRS code and message
                    new ApiErrorResult
                    {
                        Code =errorCode,
                        ErrorId = errorId,
                        Message = message
                    }
                }
            };

            ResolveRefitErrorIfAny(_exception.InnerException, error);
            return JsonUtils<ApiErrorListResultOutput>.Serialize(error);


        }
        private void ResolveRefitErrorIfAny(Exception exception, ApiErrorListResultOutput errors)
        {
            try
            {
                if (errors?.Errors == null) return;
                foreach (var error in errors.Errors)
                {
                    switch (exception)
                    {
                        case ValidationApiException validationApiError:
                            error.Code = (int?)validationApiError?.StatusCode ?? error.Code;
                            error.Message = validationApiError.Content?.Detail?.ToString() ?? error.Message;
                            break;

                        case ApiException apiException:
                            var refitError = apiException;
                            error.Code = (int?)refitError?.StatusCode ?? error.Code;
                            error.Message =
                                JsonUtils<ApiErrorResultOutput>.Deserialize(refitError.Content)?.Error?.Message ??
                                error.Message;
                            break;

                    }
                }
            }
            catch (JsonException ex)
            {
                Log.Error(ex.Message);
            }
        }
    }
}
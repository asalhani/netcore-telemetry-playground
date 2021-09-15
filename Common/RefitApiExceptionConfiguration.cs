using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Common
{
    class RefitApiExceptionResponse
    {
        public string Message { get; set; }
    }

    public class RefitApiExceptionConfiguration : IExceptionConfiguration
    {
        private readonly Refit.ApiException _exception;

        public RefitApiExceptionConfiguration(HttpContext context, Refit.ApiException exception)
        {
            _exception = exception;
        }
        public string Configure()
        {
            var message = _exception.GetContentAsAsync<RefitApiExceptionResponse>().Result?.Message;
            Guid errorId = Guid.NewGuid();

            int.TryParse(_exception.GetType().GetProperty("ErrorCode")?.GetValue(_exception, null).ToString(), out int errorCode);

            if (errorCode <= 0)
                errorCode = 1000;

            //Return List<Errors>
            var errors = new ApiErrorListResultOutput
            {
                Errors = new List<ApiErrorResult>()
                {                    
                    //SRS code and message
                    new ApiErrorResult()
                    {
                        Code = errorCode,
                        ErrorId = errorId,
                        Message = message
                    }
                }
            };

            return JsonUtils<ApiErrorListResultOutput>.Serialize(errors);
        }

    }
}
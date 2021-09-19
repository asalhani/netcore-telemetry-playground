using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace Common
{
    public class ServiceAggregateExceptionConfiguration : IExceptionConfiguration
    {
        private readonly ServiceAggregateException _exceptions;
        private readonly HttpContext _context;

        public ServiceAggregateExceptionConfiguration(HttpContext context, ServiceAggregateException exceptions)
        {
            _context = context;
            _exceptions = exceptions;
        }

        public string Configure(Guid? errorId = null)
        {
            errorId ??= Guid.NewGuid();
            
            var test = Activity.Current.TraceId;

            var error = new ApiErrorListResultOutput()
            {
                Errors = new List<ApiErrorResult>()
            };


            //Return List<Errors>
            foreach (var exception in _exceptions.InnerExceptions)
            {
                int.TryParse(exception.GetType().GetProperty("ErrorCode")?.GetValue(exception, null).ToString(), out int errorCode);
                //SRS code and message
                error.Errors.Add(new ApiErrorResult()
                {
                    Code = errorCode,
                    ErrorId = errorId.Value,
                    Message = exception.Message

                });

                Log.ForContext("Type", "Error")
                    .ForContext("Exception", exception, destructureObjects: true)
                    .ForContext("Request Url", _context.Request.Path.Value, destructureObjects: true)
                    .Error(exception, exception.Message + "-" + errorId, errorId);

            }

            return JsonUtils<ApiErrorListResultOutput>.Serialize(error);
        }
    }
}
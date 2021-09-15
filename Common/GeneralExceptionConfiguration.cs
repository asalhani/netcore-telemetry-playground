using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace Common
{
    public class GeneralExceptionConfiguration : IExceptionConfiguration
    {
        private readonly Exception _exception;
        private readonly bool _logException;
        private readonly HttpContext _context;
        private readonly HttpStatusCode _code;

        public GeneralExceptionConfiguration(HttpContext context, Exception exceptions, bool logException = false)
        {
            _context = context;
            _exception = exceptions;
            _logException = logException;
            _code = HttpStatusCode.BadRequest;
        }
        public string Configure()
        {
            var message = "Internal server error";
            Guid errorId = Guid.NewGuid();

            if( _logException )
            {
                Log.ForContext("Type", "Error")
                    .ForContext("Request Url", _context.Request.Path.Value, destructureObjects: true)
                    .Error(_exception, message + "-" + errorId, errorId);
            }

            //Return List<Errors>
            var errors = new ApiErrorListResultOutput
            {
                Errors = new List<ApiErrorResult>()
                {                    
                    //SRS code and message
                    new ApiErrorResult()
                    {
                        Code = 1000,
                        ErrorId = errorId,
                        Message = message
                    }
                }

            };

            return JsonUtils<ApiErrorListResultOutput>.Serialize(errors);


        }

    }
}
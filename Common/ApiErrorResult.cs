using System;

namespace Common
{
    public class ApiErrorResult
    {
        public int Code { get; set; }

        public string Message { get; set; }

        public Guid ErrorId { get; set; }
    }
}
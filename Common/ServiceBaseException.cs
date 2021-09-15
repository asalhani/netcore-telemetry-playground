using System;

namespace Common
{
    public class ServiceBaseException : Exception
    {
        public int ErrorCode { get; protected set; }
        [Obsolete]
        public ServiceBaseException(string message) : base(message)
        {
            ErrorCode = 1000;
        }

        public ServiceBaseException(string message, int errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }
        public ServiceBaseException(string message, Exception inner, int errorCode) : base(message, inner)
        {
            ErrorCode = errorCode;
        }
    }
}
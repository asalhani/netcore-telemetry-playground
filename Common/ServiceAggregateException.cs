using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Common
{
    public class ServiceAggregateException : AggregateException
    {
        public ServiceAggregateException(ReadOnlyCollection<Exception> innerExceptions)
            : base((IEnumerable<Exception>) innerExceptions)
        {
        }

        public ServiceAggregateException(IEnumerable<ServiceBaseException> innerExceptions)
            : base((IEnumerable<Exception>) innerExceptions)
        {
        }

        public ServiceAggregateException(
            string message,
            IEnumerable<ServiceBaseException> innerExceptions)
            : base(message, (IEnumerable<Exception>) innerExceptions)
        {
        }
    }
}
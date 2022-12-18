using System;

namespace CQRS.Exceptions
{
    public class AggregateException : Exception {
        public AggregateException(string message) : base(message)
        {
        }
    }

    public class HandlerException : Exception
    {
        public HandlerException(string message) : base(message)
        {
        }
    }

    public class SqlEventSourceException : Exception
    {
        public SqlEventSourceException(string message) : base(message)
        {
        }
    }

    public class ConcurrencyException : Exception
    {
        public ConcurrencyException(string message) : base(message)
        {
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQRS.Exceptions
{
    public class AggregateException : Exception {
        public AggregateException(string message) : base($"Aggregate Exception: {message}")
        {
        }
    }

    public class HandlerException : Exception
    {
        public HandlerException(string message) : base($"Handlers Exception: {message}")
        {
        }
    }

    public class SqlEventSourceException : Exception
    {
        public SqlEventSourceException(string message) : base($"Sql Event Source Exception: {message}")
        {
        }
    }
}

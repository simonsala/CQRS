using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQRS.Test.EventSources
{
    public class Config
    {
        public IConfiguration _configuration { get; }

        public Config()
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory() + "\\Config")
                .AddJsonFile( "config.json")
                .Build();
        }

        public string SqliteEventSource
        {
            get => _configuration.GetSection("ConnectionStrings:SqliteEventSource").Value;
        }

        public string SqlServerEventSource
        {
            get => _configuration.GetSection("ConnectionStrings:SqlServerEventSource").Value;
        }
    }
}

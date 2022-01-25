using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Autofac;
using CQRS.EventProcessors;
using CQRS.Events;
using CQRS.EventSources;
using Ecommerce.ReadModel;
using Ecommerce.ReadModel.Inventory.Models;
using Ecommerce.WriteModel.Inventory;
using Module = Autofac.Module;

namespace CQRS.ConsoleDebugger
{
    class Program
    {
        static async Task Main(string[] args)
        {
        }
    }
}
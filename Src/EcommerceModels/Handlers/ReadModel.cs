using System;

namespace Ecommerce.Handlers
{
    class ReadModel
    {
    }

    public interface IFakeService
    {
        public void Do(Type type);
    }
    
    public interface IReadModel
    {
        public void Do(Type type);
    }

    public class Mongo : IReadModel
    {
        public void Do(Type type)
        {
            Console.WriteLine($"Hello world read model from {type}");
        }
    }

    public class DumbFakeService : IFakeService
    {
        public void Do(Type type)
        {
            Console.WriteLine($"Hello world fake service from {type}");
        }
    }
}

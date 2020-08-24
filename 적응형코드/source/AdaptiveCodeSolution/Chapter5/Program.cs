using System;

namespace Chapter5
{
    class Program
    {
        static IComponent component;

        static void Main(string[] args)
        {
            var composite = new CompositeComponent();

            composite.AddComponent(new Leaf("L1"));
            composite.AddComponent(new Leaf("L2"));
            composite.AddComponent(new Leaf("L3"));

            component = composite;

            component.Foo();

            Console.ReadKey();

        }
    }
}

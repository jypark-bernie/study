using System;
using System.Collections.Generic;

namespace Chapter5
{
    public interface IComponent
    {
        void Foo();
    }

    public class Leaf : IComponent
    {
        public string Name { get; }

        public Leaf(string name)
        {
            Name = name;
        }

        public void Foo()
        {
            Console.WriteLine(Name);
        }
    }

    public class CompositeComponent : IComponent
    {
        private readonly ICollection<IComponent> children;

        public CompositeComponent()
        {
            children = new List<IComponent>();
        }

        public void AddComponent(IComponent component)
        {
            children.Add(component);
        }

        public void RemoveComponent(IComponent component)
        {
            children.Remove(component);
        }

        public void Foo()
        {
            foreach (var child in children)
            {
                child.Foo();
            }
        }
    }
}

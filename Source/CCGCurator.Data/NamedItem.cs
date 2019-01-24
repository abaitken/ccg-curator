using System;

namespace CCGCurator.Data
{
    public abstract class NamedItem
    {
        protected NamedItem(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Expected a value", "name");
            Name = name;
        }
        public string Name { get; }
    }
}
using System;
using System.Diagnostics;

namespace CCGCurator.Data
{
    [DebuggerDisplay("{Name}")]
    public abstract class NamedItem
    {
        protected NamedItem(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Expected a value", "name");
            Name = name;
        }

        [SqliteColumn("name", false)]
        public string Name { get; }
    }
}
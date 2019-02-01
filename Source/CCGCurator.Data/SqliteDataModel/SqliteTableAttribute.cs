using System;

namespace CCGCurator.Data
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SqliteTableAttribute : Attribute
    {
        public SqliteTableAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
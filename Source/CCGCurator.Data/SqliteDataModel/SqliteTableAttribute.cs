using System;

namespace CCGCurator.Data
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SqliteTableAttribute : Attribute
    {
        public string Name { get; }

        public SqliteTableAttribute(string name)
        {
            Name = name;
        }
    }
}
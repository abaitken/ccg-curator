using System;
using System.Reflection;

namespace CCGCurator.Data
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SqliteColumnAttribute : Attribute
    {
        public SqliteColumnAttribute(string name, bool nullable = true, int size = 255, Type customBehaviour = null)
            : this(nullable, size)
        {
            Name = name;
            CustomBehaviour = customBehaviour;
        }

        public SqliteColumnAttribute(bool nullable = true, int size = 255)
        {
            Nullable = nullable;
            Size = size;
        }

        public bool Nullable { get; }
        public string Name { get; }
        public Type CustomBehaviour { get; }
        public int Size { get; }

        public string ResolveName(PropertyInfo property)
        {
            if (string.IsNullOrEmpty(Name))
                return property.Name;

            return Name;
        }
    }
}
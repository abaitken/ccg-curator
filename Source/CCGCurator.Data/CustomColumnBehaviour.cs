using System.Reflection;

namespace CCGCurator.Data
{
    public  abstract class CustomColumnBehaviour
    {
        public abstract string MapType(SqliteColumnAttribute columnData, PropertyInfo property);
        public abstract object ResolveValue(SqliteColumnAttribute columnData, PropertyInfo property, object value);
    }
}
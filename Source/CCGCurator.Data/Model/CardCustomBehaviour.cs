using System;
using System.Reflection;
using CCGCurator.Data.SqliteDataModel;

namespace CCGCurator.Data.Model
{
    public class CardCustomBehaviour : CustomColumnBehaviour
    {
        public override string MapType(SqliteColumnAttribute columnData, PropertyInfo property)
        {
            switch (property.Name)
            {
                case nameof(Card.pHash):
                    return "varchar(255)";
                case nameof(Card.Set):
                    return "integer";
                default:
                    throw new InvalidOperationException();
            }
        }

        public override object ResolveValue(SqliteColumnAttribute columnData, PropertyInfo property, object value)
        {
            switch (property.Name)
            {
                case nameof(Card.pHash):
                    return value.ToString();
                case nameof(Card.Set):
                    return ((Set) value).SetId;
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
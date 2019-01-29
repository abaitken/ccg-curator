using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CCGCurator.Data
{
    internal class SqliteSchemaBuilder<T>
    {
        public SqliteSchemaBuilder()
        {
            var type = typeof(T);

            var tableAttribute = type.GetCustomAttribute<SqliteTableAttribute>();
            if(tableAttribute == null)
                throw new ArgumentException("Given type does not have the SqliteTable attribute");

            TableName = tableAttribute.Name;

            var columns = (from property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                let columnAttribute = CustomAttributeExtensions.GetCustomAttribute<SqliteColumnAttribute>((MemberInfo) property)
                let keyAttribute = property.GetCustomAttribute<SqliteKeyAttribute>()
                where columnAttribute != null
                select new
                {
                    property,
                    columnAttribute,
                    keyAttribute
                }).ToList();

            if (columns.Count == 0)
                throw new ArgumentException("Given type does not have any SqliteColumn attributes");

            Keys = (from column in columns
                where column.keyAttribute != null
                select column.columnAttribute.ResolveName(column.property)).ToList();

            Columns = (from column in columns
                select new
                {
                    column.property.Name,
                    Value = new Tuple<SqliteColumnAttribute, PropertyInfo>(column.columnAttribute,
                        column.property)
                }).ToDictionary(k => k.Name, v => v.Value);
        }


        private List<string> Keys { get; }
        public string TableName { get; }
        public IDictionary<string, Tuple<SqliteColumnAttribute, PropertyInfo>> Columns { get; set; }

        public string BuildCreateQuery()
        {
            var fields = new StringBuilder(Columns.Values.BuildCharacterSeparatedString(column =>
            {
                var columnData = column.Item1;
                var propertyInfo = column.Item2;
                var mappedType = MapType(propertyInfo, columnData);

                var nullText = columnData.Nullable ? string.Empty : " NOT NULL";
                return $"{columnData.ResolveName(propertyInfo)} {mappedType}{nullText}";
            }));

            if (Keys.Count != 0)
            {
                var keyFields = Keys.BuildCharacterSeparatedString();
                fields.Append($", PRIMARY KEY({keyFields})");
            }

            return $"CREATE TABLE {TableName} ({fields});";
        }

        private string MapType(PropertyInfo property, SqliteColumnAttribute columnData)
        {
            if (columnData.CustomBehaviour != null)
            {
                var customBehaviour = (CustomColumnBehaviour)Activator.CreateInstance(columnData.CustomBehaviour);
                return customBehaviour.MapType(columnData, property);
            }

            var columnType = property.PropertyType;
            if (columnType == typeof(int) || columnType == typeof(long))
                return "integer";
            if (columnType == typeof(bool))
                return "bit";
            if (columnType == typeof(string))
                return $"varchar({columnData.Size})";

            throw new NotSupportedException("Property type not supported");
        }

        public string BuildInsertQuery(T entity)
        {
            var columnData = (from item in Columns
                let column = item.Value
                let value = item.Value.Item2.GetValue(entity)
                select new
                {
                    column,
                    value
                }).ToDictionary(k => k.column, v => v.value);

            return BuildInsertQuery(columnData);
        }

        public string BuildInsertQuery(Dictionary<string, object> propertyValues)
        {
            if (propertyValues == null || propertyValues.Count == 0)
                throw new ArgumentNullException();

            var columnData = (from item in propertyValues
                let column = Columns[item.Key]
                let value = item.Value
                select new
                {
                    column,
                    value
                }).ToDictionary(k => k.column, v => v.value);

            return BuildInsertQuery(columnData);
        }

        public string BuildInsertQuery(Dictionary<Tuple<SqliteColumnAttribute, PropertyInfo>, object> propertyValues)
        {
            if (propertyValues == null || propertyValues.Count == 0)
                throw new ArgumentNullException();

            var fields = propertyValues.Keys.BuildCharacterSeparatedString(item => item.Item1.ResolveName(item.Item2));
            var values = propertyValues.BuildCharacterSeparatedString(item => ResolveValue(item.Key.Item2, item.Value, item.Key.Item1));

            return $"INSERT INTO {TableName} ({fields}) values ({values});";
        }

        private string EscapeString(string original)
        {
            if (string.IsNullOrEmpty(original))
                return original;

            return original.Replace("'", "''");
        }

        private string ResolveValue(PropertyInfo property, object value, SqliteColumnAttribute columnData)
        {
            var processedValue = value;
            if (processedValue == null)
                return $"NULL";

            if (columnData.CustomBehaviour != null)
            {
                var customBehaviour = (CustomColumnBehaviour)Activator.CreateInstance(columnData.CustomBehaviour);
                processedValue = customBehaviour.ResolveValue(columnData, property, processedValue);
            }

            if (processedValue is string)
                return $"'{EscapeString(processedValue.ToString())}'";

            if (processedValue is bool b)
                return b ? "1" : "0";
            
            return $"{processedValue}";
        }

        public string BuildSelectQuery(string[] properties = null, QueryCondition<T> condition = null, int? limit = null)
        {
            var columns = SelectColumnsGivenModelProperties(properties);
            var fields = columns.BuildCharacterSeparatedString(item => item.Item1.ResolveName(item.Item2));

            var conditionText = condition == null ? string.Empty : $" WHERE {condition.Build(this)}";

            var limitText = limit.HasValue ? $" LIMIT {limit.Value}" : string.Empty;
            return $"SELECT {fields} FROM {TableName}{conditionText}{limitText};";
        }

        private IEnumerable<Tuple<SqliteColumnAttribute, PropertyInfo>> SelectColumnsGivenModelProperties(IReadOnlyCollection<string> properties)
        {
            if (properties == null || properties.Count == 0)
                return Columns.Values;


            var columns = from name in properties
                select Columns[name];

            return columns;
        }

        public QueryCondition<T> CreateCondition(string modelPropertyName, ConditionOperator conditionOperator, object value)
        {
            return new QueryCondition<T>(modelPropertyName, conditionOperator, value);
        }

        public QueryCondition<T> CreateCondition(string modelPropertyName, ConditionOperator conditionOperator, T obj)
        {
            var column = Columns[modelPropertyName];
            var value = column.Item2.GetValue(obj);
            return CreateCondition(modelPropertyName, conditionOperator, value);
        }

        internal class QueryCondition<T>
        {
            public string ModelPropertyName { get; }
            public ConditionOperator ConditionOperator { get; }
            public object Value { get; }

            public QueryCondition(string modelPropertyName, ConditionOperator conditionOperator, object value)
            {
                ModelPropertyName = modelPropertyName;
                ConditionOperator = conditionOperator;
                Value = value;
            }

            public string Build(SqliteSchemaBuilder<T> parent)
            {
                var column = parent.Columns[ModelPropertyName];
                var columnName = column.Item1.ResolveName(column.Item2);
                var operationText = GetOperatorText();
                var value = parent.ResolveValue(column.Item2, Value, column.Item1);

                return $"{columnName} {operationText} {value}";
            }

            private string GetOperatorText()
            {
                switch (ConditionOperator)
                {
                    case ConditionOperator.Equals:
                        return "=";
                    case ConditionOperator.NotEquals:
                        return "<>";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public string BuildDeleteQuery(QueryCondition<T> condition)
        {
            var conditionText = $" WHERE {condition.Build(this)}";

            return $"DELETE FROM {TableName}{conditionText};";
        }
    }


    internal enum ConditionOperator
    {
        Equals,
        NotEquals
    }
}
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace CCGCurator.Data.SqliteDataModel
{
    public abstract class SQLiteData
    {
        private readonly SQLiteConnection connection;
        private SqliteSchemaBuilder<SchemaVersion> versionSchema;

        protected SQLiteData(string databaseFilePath)
        {
            var exists = File.Exists(databaseFilePath);

            connection = new SQLiteConnection("Data Source=" + databaseFilePath + ";Version=3;");
            connection.Open();

            CreateSchemaBuildersCore();
            CreateSchemaBuilders();
            if (!exists)
            {
                InitializeDatabaseCore();
                InitializeDatabase();
            }

            CheckVersion();
        }

        protected abstract int Version { get; }

        protected abstract void CreateSchemaBuilders();

        private void CreateSchemaBuildersCore()
        {
            versionSchema = new SqliteSchemaBuilder<SchemaVersion>();
        }

        private void InitializeDatabaseCore()
        {
            ExecuteNonQuery(versionSchema.BuildCreateQuery());
            var schemaVersion = new SchemaVersion {Version = Version};
            ExecuteNonQuery(versionSchema.BuildInsertQuery(schemaVersion));
        }

        public void Close()
        {
            connection.Close();
        }

        protected void CheckVersion()
        {
            var dbVersion = ExecuteScalarValueQuery<long>(versionSchema.BuildSelectQuery(limit: 1));

            if (dbVersion != Version)
                throw new InvalidOperationException($"Database is version '{dbVersion}', expected version '{Version}'");
        }

        protected T ExecuteScalarValueQuery<T>(string sqlQuery)
        {
            var command = connection.CreateCommand();
            command.CommandText = sqlQuery;
            var result = command.ExecuteScalar();

            return (T) result;
        }

        protected abstract void InitializeDatabase();

        protected void ExecuteNonQuery(string sqlQuery)
        {
            var command = connection.CreateCommand();
            command.CommandText = sqlQuery;
            command.ExecuteNonQuery();
        }

        protected string EscapeString(string original)
        {
            if (string.IsNullOrEmpty(original))
                return original;

            return original.Replace("'", "''");
        }

        protected SQLiteDataReader ExecuteReader(string sqlQuery)
        {
            var command = connection.CreateCommand();
            command.CommandText = sqlQuery;
            return command.ExecuteReader();
        }

        protected IEnumerable<T> ExecuteReader<T>(string query, Func<SQLiteDataReader, T> buildEntity)
        {
            var reader = ExecuteReader(query);
            if (!reader.HasRows)
                yield break;

            while (reader.Read())
            {
                yield return buildEntity(reader);
            }
        }

        [SqliteTable("version")]
        private class SchemaVersion
        {
            [SqliteKey]
            [SqliteColumn("version", false)]
            public int Version { get; set; }
        }
    }
}
using System;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;

namespace CCGCurator.Data
{
    public abstract class SQLiteData
    {
        private readonly SQLiteConnection connection;
        protected SQLiteData(string databaseFilePath)
        {
            var exists = File.Exists(databaseFilePath);

            connection = new SQLiteConnection("Data Source=" + databaseFilePath + ";Version=3;");
            connection.Open();

            if (!exists)
            {
                InitializeDatabaseCore();
                InitializeDatabase();
            }

            CheckVersion();
        }

        private void InitializeDatabaseCore()
        {
            ExecuteNonQuery("CREATE TABLE version(version integer NOT NULL, PRIMARY KEY(version));");
            ExecuteNonQuery($"INSERT INTO version (version) values ({Version});");
        }

        public void Close()
        {
            connection.Close();
        }

        protected abstract int Version { get; }
        protected void CheckVersion()
        {
            var dbVersion = ExecuteScalarValueQuery<long>($"SELECT version FROM version LIMIT 1;");

            if (dbVersion != Version)
                throw new InvalidOperationException($"Database is version '{dbVersion}', expected version '{Version}'");
        }

        protected T ExecuteScalarValueQuery<T>(string sqlQuery)
        {
            var command = connection.CreateCommand();
            command.CommandText = sqlQuery;
            var result = command.ExecuteScalar();

            return (T)result;
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
    }
}
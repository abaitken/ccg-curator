using System;
using System.Data.SQLite;
using System.IO;

namespace CCGCurator.Data
{
    public sealed class LocalCardData : IDisposable
    {
        private static readonly long version = 1;

        private readonly SQLiteConnection connection;
        public LocalCardData(string databaseFilePath)
        {
            var exists = File.Exists(databaseFilePath);

            connection = new SQLiteConnection("Data Source=" + databaseFilePath + ";Version=3;");
            connection.Open();

            if (!exists)
                InitializeDatabase();

            CheckVersion();
        }

        private void CheckVersion()
        {
            var dbVersion = ExecuteScalarValueQuery<long>($"SELECT version FROM version LIMIT 1;");

            if (dbVersion != version)
                throw new InvalidOperationException($"Local Card Database is version '{dbVersion}', expected version '{version}'");
        }

        private T ExecuteScalarValueQuery<T>(string sqlQuery)
        {
            var command = connection.CreateCommand();
            command.CommandText = sqlQuery;
            var result = command.ExecuteScalar();

            return (T)result;
        }

        private void InitializeDatabase()
        {
            ExecuteNonQuery("CREATE TABLE sets(setid integer NOT NULL, name varchar(255), code varchar(10), PRIMARY KEY(setid));");
            ExecuteNonQuery("CREATE TABLE cards(multiverseid integer NOT NULL, name varchar(255), phash varchar(255), setid integer NOT NULL, uuid varchar(50) NOT NULL, PRIMARY KEY(uuid));");
            ExecuteNonQuery("CREATE TABLE version(version integer NOT NULL, PRIMARY KEY(version));");
            ExecuteNonQuery($"INSERT INTO version (version) values ({version});");
        }

        private void ExecuteNonQuery(string sqlQuery)
        {
            var command = connection.CreateCommand();
            command.CommandText = sqlQuery;
            command.ExecuteNonQuery();
        }

        private string EscapeString(string original)
        {
            return original.Replace("'", "''");
        }

        public void AddSet(int id, string name, string code)
        {
            ExecuteNonQuery($"INSERT INTO sets (setid, name, code) values ({id}, '{EscapeString(name)}', '{EscapeString(code)}');");
        }

        public void AddCard(int multiverseId, string name, int setid, string uuid, string phash = "")
        {
            ExecuteNonQuery($"INSERT INTO cards (multiverseid, name, phash, setid, uuid) values ({multiverseId}, '{EscapeString(name)}', '{phash}', {setid}, '{uuid}');");
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    connection.Close();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}

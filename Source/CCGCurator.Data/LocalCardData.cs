using System;
using System.Data.SQLite;
using System.IO;

namespace CCGCurator.Data
{
    public sealed class LocalCardData : IDisposable
    {
        private readonly SQLiteConnection connection;
        public LocalCardData(string databaseFilePath)
        {
            var exists = File.Exists(databaseFilePath);

            // TODO : Sanitise the file path for the connection string?
            connection = new SQLiteConnection("Data Source=" + databaseFilePath + ";Version=3;");
            connection.Open();

            if (!exists)
                InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            ExecuteNonQuery("CREATE TABLE cards(multiverseid integer NOT NULL, name varchar(255), phash varchar(255), edition varchar(8), PRIMARY KEY(multiverseid))");
        }

        private void ExecuteNonQuery(string sqlQuery)
        {
            var command = connection.CreateCommand();
            command.CommandText = sqlQuery;
            command.ExecuteNonQuery();
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

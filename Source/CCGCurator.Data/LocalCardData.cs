using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace CCGCurator.Data
{
    public abstract class NamedItem
    {
        public NamedItem(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Expected a value", "name");
            Name = name;
        }
        public string Name { get; }
    }

    public class Set : NamedItem
    {
        public Set(string code, string name, int setId)
            : base(name)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Expected a value", "code");
            Code = code;
            SetId = setId;
        }

        public string Code { get; }
        public int SetId { get; }
    }

    public class Card : NamedItem
    {
        public Card(string name, int multiverseId, string uuid)
            : base(name)
        {
            MultiverseId = multiverseId;
            UUID = uuid;
            pHash = string.Empty;
        }

        public int MultiverseId { get; }
        public string UUID { get; }
        public string pHash { get; set; }
    }

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
            if (string.IsNullOrEmpty(original))
                return original;

            return original.Replace("'", "''");
        }

        public void AddSet(Set set)
        {
            ExecuteNonQuery($"INSERT INTO sets (setid, name, code) values ({set.SetId}, '{EscapeString(set.Name)}', '{EscapeString(set.Code)}');");
        }

        public void AddCard(Card card, Set set)
        {
            ExecuteNonQuery($"INSERT INTO cards (multiverseid, name, phash, setid, uuid) values ({card.MultiverseId}, '{EscapeString(card.Name)}', '{EscapeString(card.pHash)}', {set.SetId}, '{EscapeString(card.UUID)}');");
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

        public IList<Tuple<string, string, string>> CardsWithoutpHash()
        {
            var result = new List<Tuple<string, string, string>>();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT c.uuid, c.name, s.code FROM cards AS c INNER JOIN sets AS s ON c.setid = s.setid WHERE c.phash = ''";
            using (var reader = command.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var card = new Tuple<string, string, string>(reader.GetString(0), reader.GetString(1), reader.GetString(2));
                        result.Add(card);
                    }
                }
            }

            return result;
        }
    }
}

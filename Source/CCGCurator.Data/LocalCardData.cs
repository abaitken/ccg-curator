using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace CCGCurator.Data
{
    public sealed class LocalCardData
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

        public void Close()
        {
            connection.Close();
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
            ExecuteNonQuery($"INSERT INTO cards (multiverseid, name, phash, setid, uuid) values ({card.MultiverseId}, '{EscapeString(card.Name)}', '{card.pHash.ToString()}', {set.SetId}, '{EscapeString(card.UUID)}');");
        }

        public IEnumerable<Card> GetCardsWithHashes()
        {
            var command = connection.CreateCommand();
            command.CommandText = "SELECT name, multiverseid, uuid, phash FROM cards WHERE phash != '0';";
            var reader = command.ExecuteReader();
            if (!reader.HasRows)
                yield break;

            while(reader.Read())
            {
                yield return new Card(reader.GetString(0), reader.GetInt32(1), reader.GetString(2), ulong.Parse(reader.GetString(3)));
            }
        }

        public IEnumerable<Set> GetSets()
        {
            var command = connection.CreateCommand();
            command.CommandText = "SELECT setid, name, code FROM sets;";
            var reader = command.ExecuteReader();
            if (!reader.HasRows)
                yield break;

            while (reader.Read())
            {
                yield return new Set(reader.GetString(2), reader.GetString(1), reader.GetInt32(0));
            }
        }

        public void DeleteSetAndAssociatedCards(Set set)
        {
            var setId = ExecuteScalarValueQuery<long>($"SELECT setid FROM sets WHERE code ='{set.Code}' LIMIT 1;");
            ExecuteNonQuery($"DELETE FROM sets WHERE setid = {setId};");
            ExecuteNonQuery($"DELETE FROM cards WHERE setid = {setId};");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace CCGCurator.Data
{
    public sealed class LocalCardData : SQLiteData
    {
        public LocalCardData(string databaseFilePath)
            : base(databaseFilePath)
        {
        }

        protected override int Version => 1;

        protected override void InitializeDatabase()
        {
            ExecuteNonQuery("CREATE TABLE sets(setid integer NOT NULL, name varchar(255), code varchar(10), PRIMARY KEY(setid));");
            ExecuteNonQuery("CREATE TABLE cards(multiverseid integer NOT NULL, name varchar(255), phash varchar(255), setid integer NOT NULL, uuid varchar(50) NOT NULL, PRIMARY KEY(uuid));");
        }
        
        public void AddSet(Set set)
        {
            ExecuteNonQuery($"INSERT INTO sets (setid, name, code) values ({set.SetId}, '{EscapeString(set.Name)}', '{EscapeString(set.Code)}');");
        }

        public void AddCard(Card card)
        {
            var set = card.Set;
            ExecuteNonQuery($"INSERT INTO cards (multiverseid, name, phash, setid, uuid) values ({card.MultiverseId}, '{EscapeString(card.Name)}', '{card.pHash.ToString()}', {set.SetId}, '{EscapeString(card.UUID)}');");
        }

        public IEnumerable<Card> GetCardsWithHashes()
        {
            var reader = ExecuteReader("SELECT c.name, c.multiverseid, c.uuid, c.phash, s.name, s.code, s.setid FROM cards AS c INNER JOIN sets AS s ON c.setid = s.setid WHERE phash != '0';");
            if (!reader.HasRows)
                yield break;

            while(reader.Read())
            {
                var cardName = reader.GetString(0);
                var multiverseId = reader.GetInt32(1);
                var uuid = reader.GetString(2);
                var phash = ulong.Parse(reader.GetString(3));
                var setName = reader.GetString(4);
                var setCode = reader.GetString(5);
                var setId = reader.GetInt32(6);
                yield return new Card(cardName, multiverseId, uuid, phash, new Set(setCode, setName, setId));
            }
        }

        public IEnumerable<Set> GetSets()
        {
            var reader = ExecuteReader("SELECT setid, name, code FROM sets;");
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

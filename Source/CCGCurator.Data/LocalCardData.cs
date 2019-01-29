﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace CCGCurator.Data
{
    public sealed class LocalCardData : SQLiteData
    {
        private SqliteSchemaBuilder<Set> setSchemaBuilder;
        private SqliteSchemaBuilder<Card> cardSchemaBuilder;

        public LocalCardData(string databaseFilePath)
            : base(databaseFilePath)
        {
        }

        protected override void CreateSchemaBuilders()
        {
            setSchemaBuilder = new SqliteSchemaBuilder<Set>();
            cardSchemaBuilder = new SqliteSchemaBuilder<Card>();
        }

        protected override int Version => 1;

        protected override void InitializeDatabase()
        {
            ExecuteNonQuery(setSchemaBuilder.BuildCreateQuery());
            ExecuteNonQuery(cardSchemaBuilder.BuildCreateQuery());
        }
        
        public void AddSet(Set set)
        {
            ExecuteNonQuery(setSchemaBuilder.BuildInsertQuery(set));
        }

        public void AddCard(Card card)
        {
            ExecuteNonQuery(cardSchemaBuilder.BuildInsertQuery(card));
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
            var reader = ExecuteReader(setSchemaBuilder.BuildSelectQuery(new []
            {
                nameof(Set.SetId), nameof(Set.Name), nameof(Set.Code)
            }));
            if (!reader.HasRows)
                yield break;

            while (reader.Read())
            {
                yield return new Set(reader.GetString(2), reader.GetString(1), reader.GetInt32(0));
            }
        }

        public void DeleteSetAndAssociatedCards(Set set)
        {
            var setId = ExecuteScalarValueQuery<long>(setSchemaBuilder.BuildSelectQuery(new[] {nameof(Set.SetId)},
                setSchemaBuilder.CreateCondition(nameof(Set.Code), ConditionOperator.Equals, set),
                1));
            ExecuteNonQuery(setSchemaBuilder.BuildDeleteQuery(setSchemaBuilder.CreateCondition(nameof(Set.SetId), ConditionOperator.Equals, set)));
            ExecuteNonQuery(cardSchemaBuilder.BuildDeleteQuery(cardSchemaBuilder.CreateCondition(nameof(Card.Set), ConditionOperator.Equals, set)));
        }
    }
}

using System.Collections.Generic;

namespace CCGCurator.Data
{
    public sealed class CardCollection : SQLiteData
    {
        private SqliteSchemaBuilder<CollectedCard> collectionSchema;

        public CardCollection(string databaseFilePath)
            : base(databaseFilePath)
        {
        }

        protected override int Version => 1;

        protected override void CreateSchemaBuilders()
        {
            collectionSchema = new SqliteSchemaBuilder<CollectedCard>();
        }

        protected override void InitializeDatabase()
        {
            ExecuteNonQuery(collectionSchema.BuildCreateQuery());
        }

        public void Add(CollectedCard card)
        {
            ExecuteNonQuery(collectionSchema.BuildInsertQuery(card));
        }

        public void Delete(CollectedCard card)
        {
            ExecuteNonQuery(collectionSchema.BuildDeleteQuery(
                collectionSchema.CreateCondition(nameof(CollectedCard.Id), ConditionOperator.Equals, card)));
        }

        public IEnumerable<CollectedCard> GetCollection()
        {
            return ExecuteReader(collectionSchema.BuildSelectQuery(new[]
                {
                    nameof(CollectedCard.Id),
                    nameof(CollectedCard.Name),
                    nameof(CollectedCard.CardQuality),
                    nameof(CollectedCard.Foil),
                    nameof(CollectedCard.MultiverseId),
                    nameof(CollectedCard.SetCode),
                    nameof(CollectedCard.SetName),
                }),
                reader =>
                {
                    var id = reader.GetString(0);
                    var name = reader.GetString(1);
                    var cardQuality = (CardQuality) reader.GetInt32(2);
                    var foil = reader.GetBoolean(3);
                    var multiverseId = reader.GetInt32(4);
                    var setCode = reader.GetString(5);
                    var setName = reader.GetString(6);
                    return new CollectedCard(id,
                        name,
                        cardQuality,
                        foil,
                        multiverseId,
                        setCode,
                        setName);
                });
        }
    }
}
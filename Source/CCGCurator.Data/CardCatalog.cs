using System;

namespace CCGCurator.Data
{
    public sealed class CardCatalog : SQLiteData
    {
        public CardCatalog(string databaseFilePath) 
            : base(databaseFilePath)
        {
        }

        protected override void CreateSchemaBuilders()
        {
            throw new NotImplementedException();
        }

        protected override int Version => 1;

        protected override void InitializeDatabase()
        {
            ExecuteNonQuery(@"
CREATE TABLE catalog(
    id varchar(255) NOT NULL, 
    multiverseid integer NOT NULL, 
    name varchar(255) NOT NULL, 
    setcode varchar(255) NOT NULL, 
    setname varchar(255) NOT NULL, 
    cardquality int NOT NULL, 
    PRIMARY KEY(id)
);");
        }
    }

    public enum CardQuality
    {
        Unspecified = 0,
        Mint = 1,
        NearMint = 2,
        SlightDamage = 3,
        Damaged = 4
    }

    public class CatalogedCard : NamedItem
    {
        public Guid Id { get; }
        public CardQuality CardQuality { get; }
        public int MultiverseId { get; }
        public string SetCode { get; }
        public string SetName { get; }

        public CatalogedCard(Guid id, Card card, CardQuality cardQuality) 
            : base(card.Name)
        {
            Id = id;
            CardQuality = cardQuality;
            MultiverseId = card.MultiverseId;
            var set = card.Set;
            SetCode = set.Code;
            SetName = set.Name;
        }
    }
}
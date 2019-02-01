using System;
using CCGCurator.Data.SqliteDataModel;

namespace CCGCurator.Data.Model
{
    [SqliteTable("collection")]
    public class CollectedCard : NamedItem
    {
        public CollectedCard(Guid id, Card card, CardQuality cardQuality, bool foil)
            : this(id.ToString(), card.Name, cardQuality, foil, card.MultiverseId, card.Set.Code, card.Set.Name)
        {
        }

        public CollectedCard(string id, string name, CardQuality cardQuality, bool foil, int multiverseId,
            string setCode, string setName)
            : base(name)
        {
            Id = id;
            CardQuality = cardQuality;
            Foil = foil;
            MultiverseId = multiverseId;
            SetCode = setCode;
            SetName = setName;
        }

        [SqliteKey]
        [SqliteColumn("id", false)]
        public string Id { get; }

        [SqliteColumn("quality", false)] public CardQuality CardQuality { get; }

        [SqliteColumn("foil", false)] public bool Foil { get; }

        [SqliteColumn("multiverseid", false)] public int MultiverseId { get; }

        [SqliteColumn("setcode", false)] public string SetCode { get; }

        [SqliteColumn("setname", false)] public string SetName { get; }
    }
}
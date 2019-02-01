namespace CCGCurator.Data
{
    [SqliteTable("cards")]
    public class Card : NamedItem
    {
        public Card(string name, int multiverseId, string uuid, Set set)
            : this(name, multiverseId, uuid, 0, set)
        {
        }

        public Card(string name, int multiverseId, string uuid, ulong _phash, Set set)
            : base(name)
        {
            MultiverseId = multiverseId;
            UUID = uuid;
            pHash = _phash;
            Set = set;
        }

        [SqliteColumn("multiverseid", false)] public int MultiverseId { get; }

        [SqliteColumn("uuid", false, 50)]
        [SqliteKey]
        public string UUID { get; }

        [SqliteColumn("phash", false, customBehaviour: typeof(CardCustomBehaviour))]
        public ulong pHash { get; set; }

        [SqliteColumn("setid", false, customBehaviour: typeof(CardCustomBehaviour))]
        public Set Set { get; }
    }
}
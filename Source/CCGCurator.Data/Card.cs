namespace CCGCurator.Data
{
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

        public int MultiverseId { get; }
        public string UUID { get; }
        public ulong pHash { get; set; }
        public Set Set { get; }
    }
}
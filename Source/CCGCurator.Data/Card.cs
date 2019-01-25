using System.Diagnostics;

namespace CCGCurator.Data
{
    [DebuggerDisplay("{Name}")]
    public class Card : NamedItem
    {
        public Card(string name, int multiverseId, string uuid)
            : this(name, multiverseId, uuid, 0)
        {
        }

        public Card(string name, int multiverseId, string uuid, ulong _phash)
            : base(name)
        {
            MultiverseId = multiverseId;
            UUID = uuid;
            pHash = _phash;
        }

        public int MultiverseId { get; }
        public string UUID { get; }
        public ulong pHash { get; set; }
    }
}
using CCGCurator.Data;

namespace CCGCurator
{
    internal class IdentifiedCardCounter
    {
        public IdentifiedCardCounter(Card card)
        {
            Card = card;
        }

        public Card Card { get; }
        public int Occurrences { get; set; }
        public static string OccurrencesPropertyName => nameof(Occurrences);
    }
}
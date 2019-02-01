using System;
using CCGCurator.Data;

namespace CCGCurator
{
    internal class IdentifiedCardCounter
    {
        public Card Card { get; }
        public int Occurrences { get; set; }
        public static string OccurrencesPropertyName => nameof(Occurrences);

        public IdentifiedCardCounter(Card card)
        {
            Card = card;
        }
    }
}
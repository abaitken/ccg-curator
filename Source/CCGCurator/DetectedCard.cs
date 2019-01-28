using System;
using CCGCurator.Data;

namespace CCGCurator
{
    internal class DetectedCard
    {
        public Card Card { get; }
        public int Occurrences { get; set; }
        public static string OccurrencesPropertyName => nameof(Occurrences);

        public DetectedCard(Card card)
        {
            Card = card;
        }
    }
}
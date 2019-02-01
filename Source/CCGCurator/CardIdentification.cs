using System.Collections.Generic;
using System.Linq;
using CCGCurator.Common;
using CCGCurator.Common.phash;
using CCGCurator.Data;
using CCGCurator.Data.Model;
using CCGCurator.Views.Main;

namespace CCGCurator
{
    class CardIdentification
    {
        public List<IdentifiedCard> Identify(List<DetectedCard> detections, List<Card> referenceCards,
            SetFilter fromSet, List<Card> ignoreCards)
        {
            var phash = new pHash();
            var matchedCards = new List<IdentifiedCard>();
            foreach (var detectedCard in detections)
            {
                var cardHash = phash.ImageHash(detectedCard.CardBitmap);
                var bestMatch = FindBestMatch(cardHash, referenceCards, fromSet, ignoreCards);

                if (bestMatch != null)
                {
                    // highly experimental

#if NULL
                    const string tessDataDir = @".\\tessdata";

                    using (var engine = new TesseractEngine(tessDataDir, "eng", EngineMode.Default))
                    using (var image = Pix.LoadFromFile(".\\tempCard" + cardTempId + ".jpg"))
                    using (var page = engine.Process(image))
                    {
                        string text = page.GetText();
                        Console.WriteLine("DEBUG: Mean confidence: {0}", page.GetMeanConfidence());
                        Console.WriteLine("DEBUG: "+ text);
                    }
#endif
                    matchedCards.Add(new IdentifiedCard(detectedCard, bestMatch));
                }
            }

            return matchedCards;
        }

        private Card FindBestMatch(ulong needle, List<Card> referenceCards, SetFilter fromSet, List<Card> ignoreCards)
        {
            Card bestMatch = null;
            var lowestHamming = int.MaxValue;

            var phash = new pHash();
            var setComparer = new SetEqualityComparer();
            foreach (var referenceCard in referenceCards)
            {
                if (fromSet != SetFilter.All && !setComparer.Equals(referenceCard.Set, fromSet.Set))
                    continue;

                if(ignoreCards.Any() && ignoreCards.Contains(referenceCard))
                    continue;

                var hamming = phash.HammingDistance(referenceCard.pHash, needle);
                if (hamming < lowestHamming)
                {
                    lowestHamming = hamming;
                    bestMatch = referenceCard;
                }
            }

            return bestMatch;
        }
    }
}
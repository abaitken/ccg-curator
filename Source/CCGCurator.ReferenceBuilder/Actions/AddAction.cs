using System;
using System.Data.SQLite;
using System.Net;
using System.Threading.Tasks;
using CCGCurator.Common.phash;
using CCGCurator.Data.Model;
using CCGCurator.Data.ReferenceData;
using CCGCurator.ReferenceBuilder.Data;
using CCGCurator.ReferenceBuilder.ImageSources;
using Newtonsoft.Json;

namespace CCGCurator.ReferenceBuilder.Actions
{
    internal class AddAction : DataAction
    {
        public AddAction(Set set) : base(set)
        {
        }

        protected override string BuildActionText()
        {
            return $"Add '{Set.Name}' to reference database";
        }

        public override void Execute(LocalCardData localCardData, Logging logger, DualImageSource imageSource,
            RemoteCardData remoteCardData)
        {
            try
            {
                var cards = remoteCardData.GetCards(Set);
                localCardData.AddSet(Set);

                Parallel.ForEach(cards, card =>
                {
                    try
                    {
                        var image = imageSource.GetImage(card, Set);

                        var imageHashing = new pHash();
                        if (image != null) card.pHash = imageHashing.ImageHash(image);
                    }
                    catch (Exception ex)
                    {
                        logger.WriteLine($"CARD={card.Name};SET={Set.Code};EXCEPTION={ex.GetType()},{ex.Message}");
                    }

                    try
                    {
                        localCardData.AddCard(card);
                    }
                    catch (SQLiteException e4)
                    {
                        logger.WriteLine(
                            $"CARD={card.MultiverseId},{card.Name};SET={Set.Code},{Set.Name};EXCEPTION={e4.GetType()},{e4.Message}");
                    }
                });
            }
            catch (JsonReaderException e1)
            {
                logger.WriteLine($"SET={Set.Code},{Set.Name};EXCEPTION={e1.GetType()},{e1.Message}");
            }
            catch (WebException e2)
            {
                logger.WriteLine($"SET={Set.Code},{Set.Name};EXCEPTION={e2.GetType()},{e2.Message}");
            }
            catch (SQLiteException e3)
            {
                logger.WriteLine($"SET={Set.Code},{Set.Name};EXCEPTION={e3.GetType()},{e3.Message}");
            }
        }
    }
}
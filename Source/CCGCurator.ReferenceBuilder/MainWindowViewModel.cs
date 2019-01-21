using CCGCurator.Common;
using CCGCurator.Data;
using System.IO;

namespace CCGCurator.ReferenceBuilder
{
    class MainWindowViewModel : ViewModel
    {
        internal void CollectData()
        {
            var applicationSettings = new ApplicationSettings();

            if (File.Exists(applicationSettings.DatabasePath))
                File.Delete(applicationSettings.DatabasePath);

            var localCardData = new LocalCardData(applicationSettings.DatabasePath);
            var remoteCardData = new RemoteCardData();

            var sets = remoteCardData.GetSets();
            for (int setIndex = 0; setIndex < sets.Count; setIndex++)
            {
                var set = sets[setIndex];
                localCardData.AddSet(setIndex, set.Name, set.Code);

                var cards = remoteCardData.GetCards(set);

                for (int cardIndex = 0; cardIndex < cards.Count; cardIndex++)
                {
                    var card = cards[cardIndex];
                    localCardData.AddCard(card.MultiverseId, card.Name, setIndex);
                }
            }
        }
    }
}

using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using CCGCurator.Common;
using CCGCurator.Data;
using CCGCurator.Data.Model;
using CCGCurator.Data.PersonalCollection;

namespace CCGCurator.Views.Collection
{
    class CollectionWindowViewModel : ViewModel
    {
        private List<CollectedCard> cardCollection;
        private ICollectionView cardCollectionCollectionView;

        public ICollectionView CardCollectionCollectionView
        {
            get => cardCollectionCollectionView;
            set
            {
                if (cardCollectionCollectionView == value)
                    return;

                cardCollectionCollectionView = value;
                NotifyPropertyChanged();
            }
        }

        protected override void OnViewLoaded(Window window)
        {
            base.OnViewLoaded(window);

            Task.Run(() =>
            {
                var applicationSettings = new ApplicationSettings();
                var collectionData = new CardCollection(applicationSettings.CollectionDataPath);
                cardCollection = new List<CollectedCard>(collectionData.GetCollection());
                collectionData.Close();

                var collectionView = CollectionViewSource.GetDefaultView(cardCollection);
                CardCollectionCollectionView = collectionView;
            });
        }
    }
}
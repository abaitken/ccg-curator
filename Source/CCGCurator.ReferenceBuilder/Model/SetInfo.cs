using System.Drawing;
using CCGCurator.Common;
using CCGCurator.Data.Model;

namespace CCGCurator.ReferenceBuilder.Model
{
    internal class SetInfo : ViewModel
    {
        private readonly IDataActionsNotifier actionsNotifier;
        private Bitmap icon;
        private bool inDatabase;

        public SetInfo(Set set, bool inDatabase, IDataActionsNotifier actionsNotifier)
        {
            Set = set;
            this.inDatabase = inDatabase;
            this.actionsNotifier = actionsNotifier;
        }

        public string Name => $"({Set.Code}) {Set.Name}";
        public Set Set { get; }

        public bool InDatabase
        {
            get => inDatabase;
            set
            {
                if (inDatabase == value)
                    return;

                inDatabase = value;
                actionsNotifier.Update(Set, InDatabase);
                NotifyPropertyChanged();
            }
        }

        public Bitmap Icon
        {
            get => icon;
            set
            {
                if (icon == value)
                    return;

                icon = value;
                NotifyPropertyChanged();
            }
        }
    }
}
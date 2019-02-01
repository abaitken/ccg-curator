using CCGCurator.Data.Model;

namespace CCGCurator.ReferenceBuilder.Model
{
    internal class SetInfo
    {
        private readonly IDataActionsNotifier actionsNotifier;
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
                inDatabase = value;
                actionsNotifier.Update(Set, InDatabase);
            }
        }
    }
}
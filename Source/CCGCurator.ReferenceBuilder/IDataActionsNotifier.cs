using CCGCurator.Data;

namespace CCGCurator.ReferenceBuilder
{
    internal interface IDataActionsNotifier
    {
        void Update(Set set, bool include);
    }
}
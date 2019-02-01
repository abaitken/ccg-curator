using CCGCurator.Data;
using CCGCurator.Data.Model;

namespace CCGCurator.ReferenceBuilder
{
    internal interface IDataActionsNotifier
    {
        void Update(Set set, bool include);
    }
}
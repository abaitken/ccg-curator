using System.Data.SQLite;
using CCGCurator.Data.Model;
using CCGCurator.Data.ReferenceData;
using CCGCurator.ReferenceBuilder.Data;
using CCGCurator.ReferenceBuilder.ImageSources;

namespace CCGCurator.ReferenceBuilder.Actions
{
    internal class DeleteAction : DataAction
    {
        public DeleteAction(Set set) : base(set)
        {
        }

        protected override string BuildActionText()
        {
            return $"Remove '{Set.Name}' from reference database";
        }

        public override void Execute(LocalCardData localCardData, Logging logger, DualImageSource imageSource,
            RemoteCardData remoteCardData)
        {
            try
            {
                localCardData.DeleteSetAndAssociatedCards(Set);
            }
            catch (SQLiteException e3)
            {
                logger.WriteLine($"SET={Set.Code},{Set.Name};EXCEPTION={e3.GetType()},{e3.Message}");
            }
        }
    }
}
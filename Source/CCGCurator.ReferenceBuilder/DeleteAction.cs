using System;
using System.Data.SQLite;
using System.Net;
using System.Threading.Tasks;
using CCGCurator.Common;
using CCGCurator.Data;
using Newtonsoft.Json;

namespace CCGCurator.ReferenceBuilder
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

        public override void Execute(LocalCardData localCardData, Logging logger, DualImageSource imageSource, RemoteCardData remoteCardData)
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
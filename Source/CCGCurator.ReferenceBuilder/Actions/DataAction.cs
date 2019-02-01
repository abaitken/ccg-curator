using CCGCurator.Data;

namespace CCGCurator.ReferenceBuilder
{
    internal abstract class DataAction
    {
        protected DataAction(Set set)
        {
            Set = set;
        }

        public Set Set { get; }

        public override string ToString()
        {
            return BuildActionText();
        }

        protected abstract string BuildActionText();

        public abstract void Execute(LocalCardData localCardData, Logging logger, DualImageSource imageSource,
            RemoteCardData remoteCardData);
    }
}
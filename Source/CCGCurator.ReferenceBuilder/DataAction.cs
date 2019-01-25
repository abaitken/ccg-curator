using CCGCurator.Data;

namespace CCGCurator.ReferenceBuilder
{
    internal abstract class DataAction
    {
        public Set Set { get; }

        protected DataAction(Set set)
        {
            Set = set;
        }

        public override string ToString()
        {
            return BuildActionText();
        }

        protected abstract string BuildActionText();

        public abstract void Execute(LocalCardData localCardData, Logging logger, DualImageSource imageSource,
            RemoteCardData remoteCardData);
    }
}
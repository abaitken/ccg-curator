using CCGCurator.Data;

namespace CCGCurator
{
    internal class SetFilter
    {
        public Set Set { get; }

        public SetFilter(Set set)
        {
            Set = set;
        }

        public string Name
        {
            get
            {
                if (Set == null)
                    return "Any";
                return Set.Name;
            }
        }

        public static SetFilter All { get; } = new SetFilter(null);
    }
}
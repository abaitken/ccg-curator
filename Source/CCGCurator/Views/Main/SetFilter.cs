using CCGCurator.Data;
using CCGCurator.Data.Model;

namespace CCGCurator.Views.Main
{
    internal class SetFilter
    {
        public SetFilter(Set set)
        {
            Set = set;
        }

        public Set Set { get; }

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
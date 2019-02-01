using System.Collections.Generic;

namespace CCGCurator.Data.Model
{
    public class SetEqualityComparer : IEqualityComparer<Set>
    {
        public bool Equals(Set x, Set y)
        {
            if (x == null && y == null)
                return true;
            if (x == null || y == null)
                return false;
            return x.Code.Equals(y.Code);
        }

        public int GetHashCode(Set obj)
        {
            var hashCode = obj.Code.GetHashCode();
            return hashCode;
        }
    }
}
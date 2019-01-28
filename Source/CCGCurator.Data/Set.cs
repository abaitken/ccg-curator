using System;
using System.Collections;
using System.Collections.Generic;

namespace CCGCurator.Data
{
    public class Set : NamedItem
    {
        public Set(string code, string name, int setId)
            : base(name)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Expected a value", "code");
            Code = code;
            SetId = setId;
        }

        public string Code { get; }
        public int SetId { get; }
    }

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
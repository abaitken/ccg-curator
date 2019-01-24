using System;

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
}
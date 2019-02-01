using System;
using CCGCurator.Data.SqliteDataModel;

namespace CCGCurator.Data.Model
{
    [SqliteTable("sets")]
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

        [SqliteColumn("code", false, 10)] public string Code { get; }

        [SqliteKey]
        [SqliteColumn("setid", false)]
        public int SetId { get; }
    }
}
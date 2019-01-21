using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace CCGCurator.ReferenceBuilder
{
    class Set
    {
        public Set(string code, string name)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Expected a value", "code");
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Expected a value", "name");
            Code = code;
            Name = name;
        }

        public string Code { get; private set; }
        public string Name { get; private set; }
    }

    class RemoteCardData
    {
        TemporaryFileManager temporaryFileManager = new TemporaryFileManager();
        private List<Set> sets;

        public IEnumerable<Set> GetSets()
        {
            if (sets == null)
            {
                sets = new List<Set>();
                var localSetList = temporaryFileManager.GetTemporaryFileName(".json");

                using (var Client = new WebClient())
                {
                    Client.DownloadFile("https://mtgjson.com/json/SetList.json", localSetList);
                }

                using (var textReader = new StreamReader(localSetList))
                {
                    using (var jsonReader = new JsonTextReader(textReader))
                    {
                        var jsonData = JObject.ReadFrom(jsonReader);

                        //var jsonSets = JArray.Parse(jsonData.SelectTokens("$.*").ToString());
                        foreach (var set in jsonData)
                        {
                            var code = set["code"].ToString();
                            var name = set["name"].ToString();
                            sets.Add(new Set(code, name));
                        }
                    }
                }
            }

            return sets;
        }


        //https://mtgjson.com/json/{SETCODE}.json.zip
    }
}

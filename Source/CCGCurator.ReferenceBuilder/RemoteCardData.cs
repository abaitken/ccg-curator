using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace CCGCurator.ReferenceBuilder
{
    abstract class NamedItem
    {
        public NamedItem(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Expected a value", "name");
            Name = name;
        }
        public string Name { get; }
    }

    class Set : NamedItem
    {
        public Set(string code, string name)
            : base(name)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Expected a value", "code");
            Code = code;
        }

        public string Code { get; }
    }

    class Card : NamedItem
    {
        public Card(string name, int multiverseId)
            : base(name)
        {
            MultiverseId = multiverseId;
        }

        public int MultiverseId { get; }
    }

    class RemoteCardData
    {
        TemporaryFileManager temporaryFileManager = new TemporaryFileManager();

        public IList<Set> GetSets()
        {
            var sets = new List<Set>();
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

                    foreach (var set in jsonData)
                    {
                        var code = set["code"].ToString();
                        var name = set["name"].ToString();
                        sets.Add(new Set(code, name));
                    }
                }
            }

            return sets;
        }

        internal IList<Card> GetCards(Set set)
        {
            var cards = new List<Card>();
            var localCardList = temporaryFileManager.GetTemporaryFileName(".json");

            using (var Client = new WebClient())
            {
                Client.DownloadFile($"https://mtgjson.com/json/{set.Code}.json", localCardList);
            }

            using (var textReader = new StreamReader(localCardList))
            {
                using (var jsonReader = new JsonTextReader(textReader))
                {
                    var jsonData = JObject.ReadFrom(jsonReader);
                    var jcards = JArray.Parse(jsonData.SelectToken("$.cards").ToString());

                    foreach (var card in jcards)
                    {
                        if (card["multiverseId"] == null)
                            continue;

                        var name = card["name"].ToString();
                        var multiverseId = int.Parse(card["multiverseId"].ToString());
                        cards.Add(new Card(name, multiverseId));
                    }
                }
            }
            return cards;
        }
    }
}

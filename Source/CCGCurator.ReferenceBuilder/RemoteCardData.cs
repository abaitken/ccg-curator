using CCGCurator.Common;
using CCGCurator.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace CCGCurator.ReferenceBuilder
{

    class RemoteDataFileClient
    {
        private readonly Data.ApplicationSettings applicationSettings;
        FileSystemHelper fileSystemHelper = new FileSystemHelper();

        public RemoteDataFileClient(Data.ApplicationSettings applicationSettings)
        {
            this.applicationSettings = applicationSettings;
        }

        public string SetsFile()
        {
            var localSetList = Path.Combine(applicationSettings.SetDataCache, "SetList.json");

            if (!File.Exists(localSetList))
                using (var client = new WebClient())
                {
                    client.DownloadFile("https://mtgjson.com/json/SetList.json", localSetList);
                }

            return localSetList;
        }

        public string CardsFile(Set set)
        {
            var setFileCode = fileSystemHelper.IsInvalidFileName(set.Code) ? "set_" + set.Code : set.Code;
            var localCardList = Path.Combine(applicationSettings.SetDataCache, $"{setFileCode}.json");

            var fileInfo = new FileInfo(localCardList);
            if (!fileInfo.Exists || fileInfo.Length == 0)
                using (var client = new WebClient())
                {
                    client.DownloadFile($"https://mtgjson.com/json/{set.Code}.json", localCardList);
                }

            return localCardList;
        }
    }

    class RemoteCardData
    {
        RemoteDataFileClient remoteDataFileClient;
        public RemoteCardData(RemoteDataFileClient remoteDataFileClient)
        {
            this.remoteDataFileClient = remoteDataFileClient;;
        }
        public IList<Set> GetSets()
        {
            var sets = new List<Set>();

            using (var textReader = new StreamReader(remoteDataFileClient.SetsFile()))
            {
                using (var jsonReader = new JsonTextReader(textReader))
                {
                    var jsonData = JObject.ReadFrom(jsonReader);

                    int setId = 0;
                    foreach (var set in jsonData)
                    {
                        var code = set["code"].ToString();
                        var name = set["name"].ToString();
                        sets.Add(new Set(code, name, setId));
                        setId++;
                    }
                }
            }

            return sets;
        }

        internal IList<Card> GetCards(Set set)
        {
            var cards = new List<Card>();

            using (var textReader = new StreamReader(remoteDataFileClient.CardsFile(set)))
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
                        var uuid = card["uuid"].ToString();
                        var multiverseId = int.Parse(card["multiverseId"].ToString());
                        cards.Add(new Card(name, multiverseId, uuid, set));
                    }
                }
            }
            return cards;
        }
    }
}

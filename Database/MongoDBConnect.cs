using MongoDB.Bson;
using MongoDB.Driver;
using PacketCap;
using ServiceCore.CharacterServiceOperations;
using ServiceCore.EndPointNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using PacketCap.Database.Char;

namespace PacketCap.Database
{
    public class MongoDBConnect
    {
        public static MongoDBConnect connection;

        private IMongoDatabase db;
        private MongoClient client;
        private IMongoCollection<TradeItem> tradeCollection;
        private IMongoCollection<Character> charCollection;
        private IMongoCollection<Location> locCollection;
        private IMongoCollection<CharacterCostume> costCollection;
        private IMongoCollection<CharacterPet> petCollection;
        private IMongoCollection<Translation> transCollection;

        public MongoDBConnect()
        {
            client = new MongoClient();
            db = client.GetDatabase("MarketQuery");
            InitCollections();
        }

        public static void DropDB() {
            MongoClient client = new MongoClient();
            client.DropDatabase("MarketQuery");
        }

        public MongoDBConnect(string connString)
        {
            client = new MongoClient(connString);
            db = client.GetDatabase("MarketQuery");
            InitCollections();
        }

        public static void SetupConnect(String mongoUri) {
            if (mongoUri != null && mongoUri.Length != 0)
            {
                string url = "mongodb://" + mongoUri;
                Console.WriteLine("Using MongoDB connection string {0}",url);
                MongoDBConnect.connection = new MongoDBConnect(url);
            }
            else {
                MongoDBConnect.connection = new MongoDBConnect();
            }
        }

        public async Task ListTrades()
        {
            using (IAsyncCursor<TradeItem> cursor = await tradeCollection.FindAsync(_ => true))
            {
                while (await cursor.MoveNextAsync())
                {
                    IEnumerable<TradeItem> batch = cursor.Current;
                    foreach (TradeItem info in batch)
                    {
                        Console.WriteLine(info);
                    }
                }
            }
        }

        public async Task ListChars() {
            Console.WriteLine("Characters:");
            using (IAsyncCursor<Character> cursor = await charCollection.FindAsync(_ => true))
            {
                while (await cursor.MoveNextAsync())
                {
                    IEnumerable<Character> batch = cursor.Current;
                    foreach (Character info in batch)
                    {
                        Console.WriteLine(info);
                    }
                }
            }
        }

        public async Task ListLocations()
        {
            using (IAsyncCursor<Location> cursor = await locCollection.FindAsync(_ => true))
            {
                while (await cursor.MoveNextAsync())
                {
                    IEnumerable<Location> batch = cursor.Current;
                    foreach (Location info in batch)
                    {
                        Console.WriteLine(info);
                    }
                }
            }
        }

        public string GetAbilityString(string ability) {
            string regexExp = ability + "$";
            BsonRegularExpression regex = new BsonRegularExpression(regexExp);
            var filter = Builders<Translation>.Filter.Regex("name", regex);
            var cursor = transCollection.FindSync(filter);
            string name = "";
            string desc = "";
            while (cursor.MoveNext())
            {
                IEnumerable<Translation> batch = cursor.Current;
                foreach (Translation info in batch)
                {
                    if (info.name.Contains("_suffix_"))
                    {
                        name = info.value;
                    }
                    else if (info.name.Contains("_desc_")) {
                        desc = info.value;
                    }
                }
            }
            StringBuilder sb = new StringBuilder();
            sb.Append(name);
            sb.Append(": ");
            sb.Append(desc);
            return sb.ToString();
        }

        public void InsertTradeItemInfoList(ICollection<TradeItemInfo> infos)
        {
            if (infos == null || infos.Count == 0) {
                return;
            }
            foreach (TradeItemInfo info in infos)
            {
                TradeItem ti = new TradeItem(info);
                try
                {
                    tradeCollection.InsertOneAsync(ti).GetAwaiter().GetResult();
                }
                catch (MongoWriteException ex)
                {
                    Console.WriteLine("Insert failed: {0}", ex.Message);
                }
            }
            
        }

        public void InsertNotifyAction(NotifyAction a, long channel, int townID)
        {
            locCollection.InsertOneAsync(new Location(a,channel,townID));
        }


        public void InsertCharacterSummary(CharacterSummary c) {
            Console.WriteLine("inserting character {0}",c.CharacterID);
            charCollection.InsertOneAsync(new Character(c));
            costCollection.InsertOneAsync(new CharacterCostume(c.Costume,c.CharacterID));
            if (c.Pet != null) {
                petCollection.InsertOneAsync(new CharacterPet(c.Pet, c.CharacterID));
            }
        }

        public void InsertCharacterSummaryList(ICollection<CharacterSummary> charList)
        {
            List<Character> chars = new List<Character>(charList.Count);
            List<CharacterCostume> costumes = new List<CharacterCostume>(charList.Count);
            List<CharacterPet> pets = new List<CharacterPet>(charList.Count);

            foreach (CharacterSummary s in charList) {
                chars.Add(new Character(s));
                costumes.Add(new CharacterCostume(s.Costume, s.CharacterID));
                if (s.Pet != null) {
                    pets.Add(new CharacterPet(s.Pet, s.CharacterID));
                }
            }
            charCollection.InsertManyAsync(chars);
            costCollection.InsertManyAsync(costumes);
            if (pets.Count != 0) {
                petCollection.InsertManyAsync(pets);
            }
        }

        private void InitCollections()
        {
            List<string> collectionNames = db.ListCollectionNamesAsync().Result.ToListAsync<string>().Result;
            if (!collectionNames.Contains("Trades"))
            {
                db.CreateCollection("Trades");
            }
            if (!collectionNames.Contains("Characters")) {
                db.CreateCollection("Characters");
            }
            tradeCollection = db.GetCollection<TradeItem>("Trades");
            charCollection = db.GetCollection<Character>("Characters");
            locCollection = db.GetCollection<Location>("Locations");
            costCollection = db.GetCollection<CharacterCostume>("Costumes");
            petCollection = db.GetCollection<CharacterPet>("Pets");
            transCollection = db.GetCollection<Translation>("Translation");

            var tradeIndexKeys = Builders<TradeItem>.IndexKeys.Ascending(x => x.TID);
            var uniqueIndex = new CreateIndexOptions<TradeItem> {
                Unique = true
            };
            var indexModel = new CreateIndexModel<TradeItem>(tradeIndexKeys,uniqueIndex);
            var tradeIndex = tradeCollection.Indexes.CreateOneAsync(indexModel);

            var locationIndexKeys = Builders<Location>.IndexKeys.Descending(x => x.Time);
            var locationIndexModel = new CreateIndexModel<Location>(locationIndexKeys);
            var locationIndex = locCollection.Indexes.CreateOneAsync(locationIndexModel);

            tradeIndex.Wait();
            locationIndex.Wait();
        }
    }
}

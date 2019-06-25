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

        public void InsertTradeItemInfoList(ICollection<TradeItemInfo> infos)
        {
            if (infos == null || infos.Count == 0) {
                return;
            }
            List<TradeItem> items = new List<TradeItem>();
            foreach (TradeItemInfo info in infos)
            {
                TradeItem ti = new TradeItem(info);
                items.Add(ti);
            }
            tradeCollection.InsertManyAsync(items);
        }

        public void InsertNotifyAction(NotifyAction a, long channel, int townID)
        {
            locCollection.InsertOneAsync(new Location(a,channel,townID));
        }


        public void InsertCharacterSummary(CharacterSummary c) {
            charCollection.InsertOneAsync(new Character(c));
        }

        public void InsertCharacterSummaryList(ICollection<CharacterSummary> charList)
        {
            List<Character> list = new List<Character>(charList.Count);

            foreach (CharacterSummary s in charList) {
                list.Add(new Character(s));
            }
            charCollection.InsertManyAsync(list);
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

            var tradeBuilder = Builders<TradeItem>.IndexKeys;
            var tradeIndexKeys = tradeBuilder.Ascending(x => x.TID);
            var uniqueIndex = new CreateIndexOptions<TradeItem> {
                Unique = true
            };
            var indexModel = new CreateIndexModel<TradeItem>(tradeIndexKeys,uniqueIndex);
            var tradeIndex = tradeCollection.Indexes.CreateOneAsync(indexModel);

            var locationBuilder = Builders<Location>.IndexKeys;
            var locationIndexKeys = locationBuilder.Descending(x => x.Time);
            var locationIndex = locCollection.Indexes.CreateOneAsync(locationIndexKeys);

            tradeIndex.Wait();
            locationIndex.Wait();
        }
    }
}

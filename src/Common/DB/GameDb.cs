using MongoDB.Driver;
using N3;

namespace ProjectX.DB;

public static class GameDb
{
    private static MongoClient? _mongoClient;
    private static IMongoDatabase? _mongoDatabase;
    //public static IDatabase Rdb => _rdb;

    public static void Init(string connStr, string rdbConnStr)
    {
        SLog.Info($"init game db ...");
        MongoUrl url = new MongoUrl(connStr);
        _mongoClient = new MongoClient(url);
        _mongoDatabase = _mongoClient.GetDatabase(url.DatabaseName);
        _ = _mongoDatabase.ListCollectionNames().ToList();
        SLog.Info($"init game db ok");

        //SLog.Info($"init account rdb ...");
        //ConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(rdbConnStr);
        //_rdb = connectionMultiplexer.GetDatabase();
        //SLog.Info($"init account rdb ok");
    }
}
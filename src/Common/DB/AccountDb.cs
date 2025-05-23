using MongoDB.Driver;
using N3;
using System.Text.Json;

namespace ProjectX.DB;

public static class AccountDb
{
    private static MongoClient? _mongoClient;
    private static IMongoDatabase? _mongoDatabase;

    //private static IDatabase _rdb;

    //public static IDatabase Rdb => _rdb;

    public static void Init(string connStr, string rdbConnStr)
    {
        SLog.Info($"init account db ...");
        MongoUrl url = new MongoUrl(connStr);
        _mongoClient = new MongoClient(url);
        _mongoDatabase = _mongoClient.GetDatabase(url.DatabaseName);
        _ = _mongoDatabase.ListCollectionNames().ToList();
        SLog.Info($"init account db ok");

        //SLog.Info($"init account rdb ...");
        //ConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(rdbConnStr);
        //_rdb = connectionMultiplexer.GetDatabase();
        //SLog.Info($"init account rdb ok");


        _noticeSet = _mongoDatabase.GetCollection<NoticeData>("Notice");
    }

    private static IMongoCollection<NoticeData> _noticeSet;

    /// <summary>
    /// 加载公告信息
    /// </summary>
    /// <returns></returns>
    public static async Task<NoticeData?> LoadNotice()
    {
        NoticeData? notice = await _noticeSet.Find(f => f.Id == 0).FirstOrDefaultAsync();
        if (notice is null)
            return null;
        return notice;
    }
}
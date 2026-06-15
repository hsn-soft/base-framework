using Hhs.VideoGeneratorService.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Hhs.VideoGeneratorService.Mongo;

public sealed class VideoMongoContext
{
    private readonly IMongoDatabase _database;

    public VideoMongoContext(IOptions<MongoOptions> options)
    {
        var client = new MongoClient(options.Value.ConnectionString);
        _database = client.GetDatabase(options.Value.DatabaseName);
    }

    public IMongoCollection<VideoRequest> VideoRequests =>
        _database.GetCollection<VideoRequest>("video_requests");

    public IMongoCollection<AudioRequest> AudioRequests =>
        _database.GetCollection<AudioRequest>("audio_requests");

    public IMongoCollection<VideoGeneratorInboxMessage> InboxMessages =>
        _database.GetCollection<VideoGeneratorInboxMessage>("inbox_messages");
}
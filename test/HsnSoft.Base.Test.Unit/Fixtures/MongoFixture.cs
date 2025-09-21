using Mongo2Go;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace HsnSoft.Base.Test.Unit.Fixtures;

public class MongoFixture : IDisposable
{
    public MongoDbRunner Runner { get; }
    private bool _disposed;

    public MongoFixture()
    {
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        Runner = MongoDbRunner.Start();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // managed resources cleanup
            Runner?.Dispose();
        }

        // unmanaged resources cleanup
        _disposed = true;
    }

    ~MongoFixture()
    {
        Dispose(false);
    }
}
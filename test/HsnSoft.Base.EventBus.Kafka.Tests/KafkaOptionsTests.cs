using Confluent.Kafka;

namespace HsnSoft.Base.EventBus.Kafka.Tests;

public class KafkaOptionsTests
{
    [Fact]
    public void ByDefault_TheClientsAreMadeWithTheServersAndNoSecurityElse()
    {
        ProducerConfig config = new KafkaOptions { BootstrapServers = "kafka:9092" }.Apply(new ProducerConfig());

        Assert.Equal("kafka:9092", config.BootstrapServers);
        Assert.Equal(SecurityProtocol.Plaintext, config.SecurityProtocol);
        Assert.Null(config.SaslUsername);
        Assert.Null(config.SslCaLocation);
    }

    [Fact]
    public void TheSecurityOfTheSettings_ReachesEveryKindOfClient()
    {
        KafkaOptions options = new()
        {
            BootstrapServers = "kafka:9093",
            SecurityProtocol = SecurityProtocol.SaslSsl,
            SaslMechanism = SaslMechanism.ScramSha512,
            SaslUsername = "orders",
            SaslPassword = "secret",
            SslCaLocation = "/certs/ca.pem",
            SslCertificateLocation = "/certs/client.pem",
            SslKeyLocation = "/certs/client.key",
            SslKeyPassword = "key-secret",
        };

        foreach (ClientConfig config in new ClientConfig[] { options.Apply(new ProducerConfig()), options.Apply(new ConsumerConfig()), options.Apply(new AdminClientConfig()) })
        {
            Assert.Equal(
                ("kafka:9093", SecurityProtocol.SaslSsl, SaslMechanism.ScramSha512, "orders", "secret", "/certs/ca.pem", "/certs/client.pem", "/certs/client.key", "key-secret"),
                (config.BootstrapServers, config.SecurityProtocol, config.SaslMechanism, config.SaslUsername, config.SaslPassword, config.SslCaLocation, config.SslCertificateLocation, config.SslKeyLocation, config.SslKeyPassword));
        }
    }

    [Fact]
    public void WhatTheCallerSetOnTheConfiguration_IsNotWipedByAnUnsetOption()
    {
        ProducerConfig config = new KafkaOptions().Apply(new ProducerConfig { SaslUsername = "kept", Acks = Acks.All });

        Assert.Equal("kept", config.SaslUsername);
        Assert.Equal(Acks.All, config.Acks);
    }

    [Fact]
    public void TheSizeOfAMessage_ReachesTheProducerAndTheConsumerThatReadIt()
    {
        KafkaOptions options = new() { MessageMaxBytes = 20_000_000, FetchMaxBytes = 60_000_000, MaxPartitionFetchBytes = 25_000_000 };

        ProducerConfig producer = options.Apply(new ProducerConfig());
        ConsumerConfig consumer = options.Apply(new ConsumerConfig());

        Assert.Equal(20_000_000, producer.MessageMaxBytes);
        Assert.Equal(60_000_000, consumer.FetchMaxBytes);
        Assert.Equal(25_000_000, consumer.MaxPartitionFetchBytes);
    }

    [Fact]
    public void WithNoSizeInTheSettings_TheClientsKeepTheirDefaults()
    {
        KafkaOptions options = new();

        Assert.Null(options.Apply(new ProducerConfig()).MessageMaxBytes);
        Assert.Null(options.Apply(new ConsumerConfig()).FetchMaxBytes);
        Assert.Null(options.Apply(new ConsumerConfig()).MaxPartitionFetchBytes);
    }

    [Theory]
    [InlineData(0, null, null)]
    [InlineData(null, -1, null)]
    [InlineData(null, null, 0)]
    public void ASizeThatIsNotPositive_IsAnError(int? message, int? fetch, int? partition)
    {
        KafkaOptions options = new() { MessageMaxBytes = message, FetchMaxBytes = fetch, MaxPartitionFetchBytes = partition };

        Assert.Throws<InvalidOperationException>(() =>
        {
            options.Apply(new ProducerConfig());
            options.Apply(new ConsumerConfig());
        });
    }
}

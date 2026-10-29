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
}

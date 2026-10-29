using System;
using Confluent.Kafka;

namespace HsnSoft.Base.EventBus.Kafka;

/// <summary>Connection and topology settings; section <c>Kafka</c>.</summary>
public sealed class KafkaOptions
{
    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>How long a consumer that failed on its own (the broker could not be read, an offset could not be committed) waits before it starts again.</summary>
    public TimeSpan RecoveryDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>The longest a consumer that keeps failing waits before it starts again: the wait doubles from <see cref="RecoveryDelay"/> up to this, and starts from <see cref="RecoveryDelay"/> again after a run that lasted longer than this.</summary>
    public TimeSpan RecoveryMaxDelay { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>How long the broker waits for a sign of life of a consumer (its heartbeat) before it takes the partitions away and gives them to another instance.</summary>
    public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromSeconds(45);

    /// <summary>
    /// The longest a consumer may go without reading before the broker takes its partitions away (and what it was handling is handled again by another instance). A consumer whose places are all taken by slow handlers does not read: this must be
    /// longer than the slowest handler, and longer than <see cref="EventBusOptions.ShutdownTimeout"/> (the consumer waits that long for what it handles when partitions move).
    /// </summary>
    public TimeSpan MaxPollInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Partitions of a topic the library has to create.</summary>
    public int Partitions { get; set; } = 1;

    /// <summary>Replication factor of a topic the library has to create.</summary>
    public short ReplicationFactor { get; set; } = 1;

    /// <summary>How the client talks to the brokers. <see cref="Confluent.Kafka.SecurityProtocol.Plaintext"/> sends everything, credentials included, unencrypted: use <c>Ssl</c> or <c>SaslSsl</c> outside a closed network.</summary>
    public SecurityProtocol SecurityProtocol { get; set; } = SecurityProtocol.Plaintext;

    /// <summary>How the client signs in when the protocol is <c>SaslPlaintext</c> or <c>SaslSsl</c> (<c>Plain</c>, <c>ScramSha256</c>, <c>ScramSha512</c>).</summary>
    public SaslMechanism? SaslMechanism { get; set; }

    public string? SaslUsername { get; set; }

    /// <summary>A secret: keep it out of the settings file (environment, secret store).</summary>
    public string? SaslPassword { get; set; }

    /// <summary>The file of the certificate authority that signed the certificate of the brokers; without it the client uses the certificates of the machine.</summary>
    public string? SslCaLocation { get; set; }

    /// <summary>The certificate and key of the client, when the brokers ask for one (mutual TLS).</summary>
    public string? SslCertificateLocation { get; set; }

    public string? SslKeyLocation { get; set; }

    /// <summary>A secret: keep it out of the settings file.</summary>
    public string? SslKeyPassword { get; set; }

    /// <summary>Puts the connection settings (the servers and the security) into the configuration of a producer, a consumer or an admin client: every client of the library is made with this, so none of them is left unencrypted.</summary>
    internal T Apply<T>(T config)
        where T : ClientConfig
    {
        config.BootstrapServers = BootstrapServers;
        config.SecurityProtocol = SecurityProtocol;
        if (SaslMechanism is not null)
        {
            config.SaslMechanism = SaslMechanism;
        }

        config.SaslUsername = SaslUsername ?? config.SaslUsername;
        config.SaslPassword = SaslPassword ?? config.SaslPassword;
        config.SslCaLocation = SslCaLocation ?? config.SslCaLocation;
        config.SslCertificateLocation = SslCertificateLocation ?? config.SslCertificateLocation;
        config.SslKeyLocation = SslKeyLocation ?? config.SslKeyLocation;
        config.SslKeyPassword = SslKeyPassword ?? config.SslKeyPassword;
        return config;
    }
}

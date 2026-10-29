using System;
using System.Security.Authentication;
using HsnSoft.Base.EventBus.RabbitMQ.Configs;
using RabbitMQ.Client;

namespace HsnSoft.Base.EventBus.RabbitMQ.Connection;

/// <summary>Makes the connection factory every connection of the library is opened with (the persistent one and the one of the health check), so none of them is left without the security of the settings.</summary>
internal static class RabbitMqConnectionFactory
{
    internal static ConnectionFactory Create(RabbitMqConnectionSettings settings, bool automaticRecovery)
    {
        ConnectionFactory factory = new()
        {
            HostName = settings.HostName,
            Port = settings.Port,
            UserName = settings.UserName,
            Password = settings.Password,
            VirtualHost = settings.VirtualHost,
            RequestedHeartbeat = TimeSpan.FromSeconds(60),
        };

        if (automaticRecovery)
        {
            // the client reconnects forever by itself and recovers channels and consumers
            factory.AutomaticRecoveryEnabled = true;
            factory.TopologyRecoveryEnabled = true;
            factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(10);
            factory.HandshakeContinuationTimeout = TimeSpan.FromMinutes(2);
            factory.ContinuationTimeout = TimeSpan.FromMinutes(2);
        }
        else
        {
            // a connection that is opened to be closed at once (the health check) has nothing to recover
            factory.AutomaticRecoveryEnabled = false;
            factory.TopologyRecoveryEnabled = false;
        }

        if (settings.UseTls)
        {
            factory.Ssl = new SslOption
            {
                Enabled = true,
                ServerName = string.IsNullOrWhiteSpace(settings.TlsServerName) ? settings.HostName : settings.TlsServerName,
                Version = SslProtocols.Tls12 | SslProtocols.Tls13,
                CertPath = settings.ClientCertificatePath ?? string.Empty,
                CertPassphrase = settings.ClientCertificatePassword,
            };
        }

        return factory;
    }
}

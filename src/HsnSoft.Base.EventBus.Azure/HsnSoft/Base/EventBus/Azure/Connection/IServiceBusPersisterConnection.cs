using System;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace HsnSoft.Base.EventBus.Azure.Connection;

public interface IServiceBusPersisterConnection : IDisposable
{
    ServiceBusClient TopicClient { get; }
    ServiceBusAdministrationClient AdministrationClient { get; }
}
// using MediatR;

namespace HsnSoft.Base.Domain.Entities;

public class DomainEventRecord 
{
    public object EventData { get; }

    public long EventOrder { get; }

    public DomainEventRecord(object eventData, long eventOrder)
    {
        EventData = eventData;
        EventOrder = eventOrder;
    }
}

// public class DomainEventRecord :INotification
// {
//     public object EventData { get; }
//
//     public long EventOrder { get; }
//
//     public DomainEventRecord(object eventData, long eventOrder)
//     {
//         EventData = eventData;
//         EventOrder = eventOrder;
//     }
// }
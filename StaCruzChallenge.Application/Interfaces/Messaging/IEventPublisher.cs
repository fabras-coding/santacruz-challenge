using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StaCruzChallenge.Application.Interfaces.Messaging
{
    public interface IEventPublisher
    {
        Task PublishAsync(string eventType,string payload, CancellationToken cancellationToken);
    }
}
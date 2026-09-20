using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using StaCruzChallenge.Domain.Entities;

namespace StaCruzChallenge.Domain.Repositories
{
    public interface IOutboxOrderRepository
    {
        
        
        Task<IReadOnlyList<OutboxOrderMessage>> GetPendingAsync(CancellationToken cancellationToken);
        Task MarkAsProcessedAsync(Guid id, CancellationToken cancellationToken);
        Task MarkAsFailedAsync(Guid id, CancellationToken cancellationToken);
        Task IncrementAttemptsAsync(Guid id, CancellationToken cancellationToken);

    }
}
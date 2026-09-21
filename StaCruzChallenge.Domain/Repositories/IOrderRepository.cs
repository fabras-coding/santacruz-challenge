using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StaCruzChallenge.Domain.Entities;

namespace StaCruzChallenge.Domain.Repositories
{
    public interface IOrderRepository
    {
        Task<long> CreateAsync(Order order, OutboxOrderMessage outboxMessage, CancellationToken cancellationToken);

        Task<Order?> GetByIdAsync(long id, CancellationToken cancellationToken);
        Task<IEnumerable<Order>> GetAllPaginatedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);

        Task UpdateStatusAsync(long id, string status, CancellationToken cancellationToken);

    }
}
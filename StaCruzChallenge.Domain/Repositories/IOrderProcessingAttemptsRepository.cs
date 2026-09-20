using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StaCruzChallenge.Domain.Entities;

namespace StaCruzChallenge.Domain.Repositories
{
    public interface IOrderProcessingAttemptsRepository
    {
        Task AddAsync(OrderProcessingAttempts attempt, CancellationToken cancellationToken);

        Task<IReadOnlyList<OrderProcessingAttempts>> GetByOrderIdAsync(long orderId, CancellationToken cancellationToken);
    }
}
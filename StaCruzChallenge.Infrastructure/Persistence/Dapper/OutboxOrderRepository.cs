using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using StaCruzChallenge.Domain.Entities;
using StaCruzChallenge.Domain.Repositories;
using StaCruzChallenge.Infrastructure.Persistence.Data;

namespace StaCruzChallenge.Infrastructure.Persistence.Dapper
{
    public class OutboxOrderRepository : IOutboxOrderRepository
    {

        private readonly IPostgresConnectionFactory _connectionFactory;
        private readonly ILogger<OutboxOrderRepository> _logger;


        public OutboxOrderRepository(IPostgresConnectionFactory connectionFactory, ILogger<OutboxOrderRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }


        public async  Task<IReadOnlyList<OutboxOrderMessage>> GetPendingAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async  Task IncrementAttemptsAsync(Guid id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task MarkAsFailedAsync(Guid id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task MarkAsProcessedAsync(Guid id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
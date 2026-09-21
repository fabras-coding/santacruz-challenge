using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using StaCruzChallenge.Domain.Entities;
using StaCruzChallenge.Domain.Enums;
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


        public async  Task<IReadOnlyList<OutboxOrderMessage>> GetPendingAsync(int size, CancellationToken cancellationToken)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken: cancellationToken);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
    
            //TODO: Improve to not have Pending messages for too long without being processed


            var query = @"SELECT obo_id AS Id, obo_event_type AS EventType, obo_payload AS Payload, obo_status AS Status, obo_attempts AS Attempts, obo_createdat AS CreatedAt
                          FROM outbox_orders
                          WHERE obo_status = 'Pending'
                          ORDER BY obo_createdat
                          LIMIT @Size
                          FOR UPDATE SKIP LOCKED";


            var result = await connection.QueryAsync<OutboxOrderMessage>(query,
             new { Size = size }, transaction: transaction );


            var updateQuery = @"UPDATE outbox_orders
                     SET obo_status = @Status
                     WHERE obo_id = ANY(@Ids)";

            var ids = result.Select(r => r.Id).ToArray();
            
            if (ids.Length > 0)
            {
                await connection.ExecuteAsync(updateQuery,
                 new { Ids = ids, Status = OrderStatus.InProgress.ToString() }, transaction: transaction );
            }

            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("Retrieved {ItemCount} pending outbox order messages.", result.Count());


            return result.ToList();
        }

        public async  Task IncrementAttemptsAsync(Guid id, CancellationToken cancellationToken)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken: cancellationToken);

            var query = @"UPDATE outbox_orders
                          SET obo_attempts = obo_attempts + 1
                          WHERE obo_id = @Id";

            await connection.ExecuteAsync(query,
             new { Id = id });
        }

        public async Task MarkAsFailedAsync(Guid id, CancellationToken cancellationToken)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken: cancellationToken);

            var query = @"UPDATE outbox_orders
                          SET obo_status = @Status
                          WHERE obo_id = @Id";

            await connection.ExecuteAsync(query,
             new { Id = id, Status = OrderStatus.Failed.ToString() });
        }

        public async Task MarkAsEnqueuedAsync(Guid id, CancellationToken cancellationToken)
        {
            
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken: cancellationToken);

            var query = @"UPDATE outbox_orders
                          SET obo_status = @Status
                          WHERE obo_id = @Id";

            await connection.ExecuteAsync(query,
             new { Id = id, Status = OrderStatus.Enqueued.ToString() });


        }

        public async Task MarkAsProcessedAsync(Guid id, CancellationToken cancellationToken)
        {
            
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken: cancellationToken);

            var query = @"UPDATE outbox_orders
                          SET obo_status = @Status
                          WHERE obo_id = @Id";

            await connection.ExecuteAsync(query,
             new { Id = id, Status = OrderStatus.Processed.ToString() });


        }


    }
}
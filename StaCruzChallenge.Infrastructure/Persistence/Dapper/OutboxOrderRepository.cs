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


        public async Task<IReadOnlyList<OutboxOrderMessage>> GetPendingAsync(int size, int forgottenMessagesIntervalMinutes, CancellationToken cancellationToken)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken: cancellationToken);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            var query = "";
            if(forgottenMessagesIntervalMinutes > 0){
                query = @"SELECT obo_id AS Id, obo_event_type AS EventType, obo_payload AS Payload, obo_status AS Status, obo_createdat AS CreatedAt
                          FROM outbox_orders
                          WHERE obo_status <> 'Completed'
                          AND obo_createdat <= NOW() - (@ForgottenMessagesIntervalMinutes * INTERVAL '1 minute')
                          ORDER BY obo_createdat
                          LIMIT @Size
                          FOR UPDATE SKIP LOCKED";

            }
            else
            {
                query = @"SELECT obo_id AS Id, obo_event_type AS EventType, obo_payload AS Payload, obo_status AS Status, obo_createdat AS CreatedAt
                          FROM outbox_orders
                          WHERE obo_status = 'Pending'
                          ORDER BY obo_createdat
                          LIMIT @Size
                          FOR UPDATE SKIP LOCKED";
            }

            

            var result = await connection.QueryAsync<OutboxOrderMessage>(query,
            forgottenMessagesIntervalMinutes > 0 ? new { Size = size, ForgottenMessagesIntervalMinutes = forgottenMessagesIntervalMinutes } : new { Size = size }, transaction: transaction);

            if(result is null || !result.Any())
            {
                _logger.LogInformation("No pending outbox order messages found.");
                return new List<OutboxOrderMessage>();
            }

            var updateQuery = @"UPDATE outbox_orders
                     SET obo_status = @Status
                     WHERE obo_id = ANY(@Ids)";

            var ids = result.Select(r => r.Id).ToArray();

            if (ids.Length > 0)
            {
                await connection.ExecuteAsync(updateQuery,
                 new { Ids = ids, Status = OrderStatus.Processing.ToString() }, transaction: transaction);
            }

            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("Retrieved {ItemCount} pending outbox order messages.", result.Count());
            _logger.LogInformation("Updated status to Processing for {ItemCount} outbox order messages.", ids.Length);


            return result.ToList();
        }


        public async Task MarkAsFailedAsync(Guid id, CancellationToken cancellationToken)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken: cancellationToken);

            var query = @"UPDATE outbox_orders
                          SET obo_status = @Status, obo_processedat = @Now
                          WHERE obo_id = @Id";

            await connection.ExecuteAsync(query,
             new { Id = id, Status = OrderStatus.Failed.ToString(), Now = DateTime.Now });
        }

        public async Task MarkAsEnqueuedAsync(Guid id, CancellationToken cancellationToken)
        {

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken: cancellationToken);

            var query = @"UPDATE outbox_orders
                          SET obo_status = @Status, obo_processedat = @Now
                          WHERE obo_id = @Id";

            await connection.ExecuteAsync(query,
             new { Id = id, Status = OrderStatus.Enqueued.ToString(), Now = DateTime.Now });


        }

        public async Task MarkAsProcessedAsync(Guid id, CancellationToken cancellationToken)
        {

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken: cancellationToken);

            var query = @"UPDATE outbox_orders
                          SET obo_status = @Status, obo_processedat = @Now
                          WHERE obo_id = @Id";

            await connection.ExecuteAsync(query,
             new { Id = id, Status = OrderStatus.Completed.ToString(), Now = DateTime.Now });


        }


    }
}
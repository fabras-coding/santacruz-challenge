using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using StaCruzChallenge.Domain.Entities;
using StaCruzChallenge.Domain.Repositories;
using StaCruzChallenge.Infrastructure.Persistence.Data;

namespace StaCruzChallenge.Infrastructure.Persistence.Dapper
{
    public class OrderProcessingAttemptsRepository : IOrderProcessingAttemptsRepository
    {

        private readonly IPostgresConnectionFactory _connectionFactory;
        private readonly ILogger<OrderProcessingAttemptsRepository> _logger;

        public OrderProcessingAttemptsRepository(IPostgresConnectionFactory connectionFactory, ILogger<OrderProcessingAttemptsRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task AddAsync(OrderProcessingAttempts attempt, CancellationToken cancellationToken)
        {
            
            const string query = @"
            INSERT INTO order_processing_attempts (opa_id, opa_order_id, opa_startedat, opa_endedat,  
            opa_attempt_number, opa_success, opa_error_message)
            VALUES (@Id, @OrderId, @StartedAt, @EndedAt, @AttemptNumber, @Success, @ErrorMessage)";

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await connection.ExecuteAsync(query, new
            {
                Id = attempt.Id,
                OrderId = attempt.OrderId,
                StartedAt = attempt.StartedAt,
                EndedAt = attempt.EndedAt,
                AttemptNumber = attempt.AttemptNumber,
                Success = attempt.Success,
                ErrorMessage = attempt.ErrorMessage
            });

            _logger.LogInformation("Order processing attempt added successfully for OrderId: {OrderId}", attempt.OrderId);


        }

        public async Task<IReadOnlyList<OrderProcessingAttempts>> GetByOrderIdAsync(long orderId, CancellationToken cancellationToken)
        {
            
            const string query = @"
            SELECT opa_id AS Id, opa_order_id AS OrderId, opa_startedat AS StartedAt, opa_endedat AS EndedAt,
                   opa_attempt_number AS AttemptNumber, opa_success AS Success, opa_error_message AS ErrorMessage
            FROM order_processing_attempts
            WHERE opa_order_id = @OrderId";

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            var attempts = await connection.QueryAsync<OrderProcessingAttempts>(query, new { OrderId = orderId });
            return attempts.ToList();

        }
    }
}
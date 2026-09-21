using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StaCruzChallenge.Domain.Entities;
using StaCruzChallenge.Domain.Repositories;
using StaCruzChallenge.Infrastructure.Persistence.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using System.Text.Json;


namespace StaCruzChallenge.Infrastructure.Persistence.Dapper
{
    public class OrderRepository : IOrderRepository
    {

        private readonly ILogger<OrderRepository> _logger;
        private readonly IPostgresConnectionFactory _connectionFactory;

        public OrderRepository(IPostgresConnectionFactory connectionFactory, ILogger<OrderRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<long> CreateAsync(Order order, OutboxOrderMessage outboxMessage, CancellationToken cancellationToken)
        {

            const string insertOrderSql = @"
            INSERT INTO orders (o_user_id, o_createdat, o_updatedat, o_total_amount, o_status)
            VALUES (@UserId, @CreatedAt, @UpdatedAt, @TotalAmount, @Status)
            RETURNING o_id;
            ";

            const string insertItemSql = @"
            INSERT INTO order_items (oi_id, oi_order_id, oi_product_id, oi_unit_value, oi_quantity, oi_total_amount)
            VALUES (@Id, @OrderId, @ProductId, @UnitValue, @Quantity, @TotalAmount);
            ";

            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync(cancellationToken: cancellationToken);

            await using var transaction = await conn.BeginTransactionAsync(cancellationToken);

            try
            {
                var orderId = await conn.ExecuteScalarAsync<long>(
                    new CommandDefinition(insertOrderSql, new
                    {
                        UserId = order.UserId,
                        CreatedAt = order.CreatedAt,
                        UpdatedAt = order.CreatedAt,
                        TotalAmount = order.TotalAmount,
                        Status = order.Status
                    }, transaction: transaction, cancellationToken: cancellationToken)
                );

                order.Id = orderId;

                foreach (var item in order.Items!)
                {

                    item.Id = Guid.NewGuid();
                    item.OrderId = orderId;
                    var param = new
                    {
                        Id = item.Id,
                        OrderId = item.OrderId,
                        ProductId = item.ProductId,
                        UnitValue = item.UnitValue,
                        Quantity = item.Quantity,
                        TotalAmount = item.TotalAmount
                    };

                    await conn.ExecuteAsync(
                        new CommandDefinition(insertItemSql, param, transaction: transaction, cancellationToken: cancellationToken)
                    );
                }


                await conn.ExecuteAsync(
                    new CommandDefinition(
                        @"INSERT INTO outbox_orders (obo_id, obo_event_type, obo_payload, obo_status, obo_attempts, obo_createdat, obo_processedat)
                          VALUES (@Id, @EventType, @Payload::jsonb, @Status, @Attempts, @CreatedAt, NULL);",
                        new
                        {
                            Id = outboxMessage.Id,
                            CreatedAt = outboxMessage.CreatedAt,
                            Payload = JsonSerializer.Serialize(order),
                            EventType = outboxMessage.EventType,
                            Status = outboxMessage.Status,
                            Attempts = outboxMessage.Attempts
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken
                    )
                );

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Order {OrderId} created successfully.", orderId);

                return orderId;
            }
            catch
            {
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError("Failed to rollback transaction.");
                }
                catch
                {
                    _logger.LogError("An error occurred while rolling back the transaction.");
                }
                throw;
            }
            finally
            {
                // connection and transaction disposed by await using
            }

        }

        public async Task<IEnumerable<Order>> GetAllPaginatedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
        {
            const string getAllPaginatedSql = @"
            SELECT
            o.o_id        AS Id,
            o.o_user_id   AS UserId,
            o.o_createdat AS CreatedAt,
            o.o_updatedat AS UpdatedAt,
            o.o_total_amount AS TotalAmount,
            o.o_status    AS Status,
            
            oi.oi_id      AS ItemId,
            oi.oi_order_id AS OrderId,
            oi.oi_product_id AS ProductId,
            oi.oi_quantity   AS Quantity,
            oi.oi_unit_value AS UnitValue,
            oi.oi_total_amount AS TotalAmount

            FROM orders o
            LEFT JOIN order_items oi ON oi.oi_order_id = o.o_id
            ORDER BY o.o_createdat DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            ";

            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync(cancellationToken);

            var orders = new Dictionary<long, Order>();

            await conn.QueryAsync<Order, OrderItem, Order>(
                new CommandDefinition(getAllPaginatedSql, new { Offset = (pageNumber - 1) * pageSize, PageSize = pageSize }, cancellationToken: cancellationToken),
                (order, item) =>
                {
                    if (!orders.TryGetValue(order.Id, out var existingOrder))
                    {
                        existingOrder = order;
                        existingOrder.Items = new List<OrderItem>();
                        orders.Add(order.Id, existingOrder);
                    }

                    if (item is not null && item.Id != Guid.Empty)
                    {
                        existingOrder.Items.Add(item);
                    }

                    return existingOrder;
                },
                splitOn: "ItemId"
            );

            return orders.Values;
        }

        public async Task<Order?> GetByIdAsync(long id, CancellationToken cancellationToken)
        {

            const string getOrderByIdSql = @"
            SELECT
            o.o_id        AS Id,
            o.o_user_id   AS UserId,
            o.o_createdat AS CreatedAt,
            o.o_updatedat AS UpdatedAt,
            o.o_total_amount AS TotalAmount,
            o.o_status    AS Status,
            
            oi.oi_id      AS ItemId,
            oi.oi_order_id AS OrderId,
            oi.oi_product_id AS ProductId,
            oi.oi_quantity   AS Quantity,
            oi.oi_unit_value AS UnitValue,
            oi.oi_total_amount AS TotalAmount

            FROM orders o
            LEFT JOIN order_items oi ON oi.oi_order_id = o.o_id
            WHERE o.o_id = @OrderId;
            ";

            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync(cancellationToken);

            var orders = new Dictionary<long, Order>();

            await conn.QueryAsync<Order, OrderItem, Order>(
                new CommandDefinition(getOrderByIdSql, new { OrderId = id }, cancellationToken: cancellationToken),
                (order, item) =>
                {
                    if (!orders.TryGetValue(order.Id, out var existingOrder))
                    {
                        existingOrder = order;
                        existingOrder.Items = new List<OrderItem>();
                        orders.Add(order.Id, existingOrder);
                    }

                    if (item is not null && item.Id != Guid.Empty)
                    {
                        existingOrder.Items.Add(item);
                    }

                    return existingOrder;
                },
                splitOn: "ItemId"
            );

            return orders.Values.SingleOrDefault();

        }

        public async Task UpdateStatusAsync(long orderId, string status, CancellationToken cancellationToken)
        {
            const string sql = "UPDATE orders SET o_status = @Status, o_updatedat = @UpdatedAt WHERE o_id = @OrderId";

            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync(cancellationToken);

            await conn.ExecuteAsync(new CommandDefinition(sql,
                new { OrderId = orderId, Status = status, UpdatedAt = DateTime.Now },
                cancellationToken: cancellationToken));
        }
    }
}
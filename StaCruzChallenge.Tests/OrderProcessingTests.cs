using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StaCruzChallenge.Application.Interfaces;
using StaCruzChallenge.Application.Interfaces.Security;
using StaCruzChallenge.Application.Interfaces.ExternalServices;
using StaCruzChallenge.Application.Products;
using StaCruzChallenge.Application.Services;
using StaCruzChallenge.Application.Orders;
using StaCruzChallenge.Domain.Entities;
using StaCruzChallenge.Domain.Repositories;
using StaCruzChallenge.Domain.Enums;
using StaCruzChallenge.Infrastructure.Workers;
using Xunit;

namespace StaCruzChallenge.Tests;

public sealed class OrderProcessingTests
{
    [Fact]
    public async Task CreateOrderAsync_uses_catalog_price_persists_order_and_returns_pending()
    {
        var productId = Guid.NewGuid();
        var repository = new InMemoryOrderRepository();
        var service = CreateOrderService(repository, [new ProductDto { Id = productId, Name = "Coffee", Price = 12.50m }]);

        var result = await service.CreateOrderAsync(
            new CreateOrderDto { Items = [new OrderItemDto { ProductId = productId, Quantity = 2 }] },
            CancellationToken.None);

        Assert.Equal("Pending", result.Status);
        var createdOrder = Assert.Single(repository.CreatedOrders);
        Assert.Equal(25.00m, createdOrder.TotalAmount);
        Assert.Equal(12.50m, createdOrder.Items!.Single().UnitValue);
        Assert.NotEqual(Guid.Empty, repository.CreatedOrders[0].UserId);
    }

    [Fact]
    public async Task CreateOrderAsync_calculates_total_for_multiple_items()
    {
        var firstProductId = Guid.NewGuid();
        var secondProductId = Guid.NewGuid();
        var repository = new InMemoryOrderRepository();
        var service = CreateOrderService(repository,
        [
            new ProductDto { Id = firstProductId, Name = "Coffee", Price = 12.50m },
            new ProductDto { Id = secondProductId, Name = "Tea", Price = 8.00m }
        ]);

        await service.CreateOrderAsync(
            new CreateOrderDto
            {
                Items =
                [
                    new OrderItemDto { ProductId = firstProductId, Quantity = 2 },
                    new OrderItemDto { ProductId = secondProductId, Quantity = 3 }
                ]
            },
            CancellationToken.None);

        Assert.Equal(49.00m, Assert.Single(repository.CreatedOrders).TotalAmount);
    }

    [Theory]
    [MemberData(nameof(InvalidOrders))]
    public async Task CreateOrderAsync_rejects_invalid_items(CreateOrderDto order)
    {
        var repository = new InMemoryOrderRepository();
        var service = CreateOrderService(repository, []);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateOrderAsync(order, CancellationToken.None));

        Assert.Empty(repository.CreatedOrders);
    }

    public static IEnumerable<object[]> InvalidOrders() =>
    [
        [new CreateOrderDto { Items = [] }],
        [new CreateOrderDto { Items = [new OrderItemDto { ProductId = Guid.NewGuid(), Quantity = 0 }] }],
        [new CreateOrderDto { Items = [new OrderItemDto { ProductId = Guid.NewGuid(), Quantity = -1 }] }],
        [new CreateOrderDto { Items = [new OrderItemDto { ProductId = Guid.NewGuid(), Quantity = 1 }] }]
    ];

    [Fact]
    public async Task OrderProcessorWorker_failed_integration_updates_order_and_records_attempt()
    {
        var repository = new InMemoryOrderRepository();
        var attempts = new InMemoryAttemptsRepository();
        var orderId = 42L;
        var worker = CreateWorker(repository, attempts, success: false);

        var result = await worker.ProcessMessageForTestAsync(
            $"{{\"OrderId\":{orderId},\"OutboxMessageId\":\"{Guid.NewGuid()}\"}}",
            CancellationToken.None);

        Assert.False(result.Item1);
        Assert.False(result.Item2);
        Assert.Equal(OrderStatus.Failed.ToString(), repository.Statuses[orderId]);
        Assert.Single(attempts.Items);
        Assert.False(attempts.Items[0].Success);
    }

    [Fact]
    public async Task OrderProcessorWorker_successful_integration_completes_order_and_outbox_message()
    {
        var repository = new InMemoryOrderRepository();
        var attempts = new InMemoryAttemptsRepository();
        var outbox = new InMemoryOutboxRepository();
        var orderId = 42L;
        var outboxMessageId = Guid.NewGuid();
        var worker = CreateWorker(repository, attempts, outbox, success: true);

        var result = await worker.ProcessMessageForTestAsync(
            $"{{\"OrderId\":{orderId},\"OutboxMessageId\":\"{outboxMessageId}\"}}",
            CancellationToken.None);

        Assert.True(result.Item1);
        Assert.Equal(OrderStatus.Completed.ToString(), repository.Statuses[orderId]);
        Assert.Contains(outboxMessageId, outbox.ProcessedIds);
        Assert.True(Assert.Single(attempts.Items).Success);
    }

    private static OrderService CreateOrderService(InMemoryOrderRepository repository, IEnumerable<ProductDto> products)
    {
        var productService = new StubProductService(products);
        var currentUser = new StubCurrentUser(Guid.NewGuid());
        return new OrderService(repository, productService, currentUser, NullLogger<OrderService>.Instance);
    }

    private static OrderProcessorWorker CreateWorker(InMemoryOrderRepository orders, InMemoryAttemptsRepository attempts, bool success)
        => CreateWorker(orders, attempts, new InMemoryOutboxRepository(), success);

    private static OrderProcessorWorker CreateWorker(InMemoryOrderRepository orders, InMemoryAttemptsRepository attempts, InMemoryOutboxRepository outbox, bool success)
    {
        var services = new ServiceCollection();
        services.AddScoped<IOrderRepository>(_ => orders);
        services.AddScoped<IOrderProcessingAttemptsRepository>(_ => attempts);
        services.AddScoped<IOutboxOrderRepository>(_ => outbox);
        services.AddScoped<IExternalOrderCaller>(_ => new StubExternalCaller(success));
        var provider = services.BuildServiceProvider();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ProcessingAttempt:MaxRetryAttempts"] = "3"
        }).Build();
        return new OrderProcessorWorker(provider.GetRequiredService<IServiceScopeFactory>(), configuration, NullLogger<OrderProcessorWorker>.Instance);
    }

    private sealed class StubProductService(IEnumerable<ProductDto> products) : IProductService
    {
        public Task<IEnumerable<ProductDto>> GetAllAsync(CancellationToken cancellationToken) => Task.FromResult(products);
    }

    private sealed class StubCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId => userId;
    }

    private sealed class StubExternalCaller(bool success) : IExternalOrderCaller
    {
        public Task<bool> ProcessOrderAsync(long orderId, CancellationToken cancellationToken) => Task.FromResult(success);
    }

    private sealed class InMemoryOrderRepository : IOrderRepository
    {
        public List<Order> CreatedOrders { get; } = [];
        public Dictionary<long, string> Statuses { get; } = [];
        public Task<long> CreateAsync(Order order, OutboxOrderMessage outboxMessage, CancellationToken cancellationToken)
        {
            order.Id = 1;
            CreatedOrders.Add(order);
            return Task.FromResult(order.Id);
        }
        public Task<Order?> GetByIdAsync(long id, CancellationToken cancellationToken) => Task.FromResult<Order?>(null);
        public Task<IEnumerable<Order>> GetAllPaginatedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken) => Task.FromResult<IEnumerable<Order>>([]);
        public Task UpdateStatusAsync(long id, string status, CancellationToken cancellationToken)
        {
            Statuses[id] = status;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryAttemptsRepository : IOrderProcessingAttemptsRepository
    {
        public List<OrderProcessingAttempts> Items { get; } = [];
        public Task AddAsync(OrderProcessingAttempts attempt, CancellationToken cancellationToken)
        {
            Items.Add(attempt);
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<OrderProcessingAttempts>> GetByOrderIdAsync(long orderId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<OrderProcessingAttempts>>(Items.Where(x => x.OrderId == orderId).ToList());
    }

    private sealed class InMemoryOutboxRepository : IOutboxOrderRepository
    {
        public List<Guid> ProcessedIds { get; } = [];
        public Task<IReadOnlyList<OutboxOrderMessage>> GetPendingAsync(int size, int forgottenMessagesIntervalMinutes, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<OutboxOrderMessage>>([]);
        public Task MarkAsEnqueuedAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task MarkAsProcessedAsync(Guid id, CancellationToken cancellationToken)
        {
            ProcessedIds.Add(id);
            return Task.CompletedTask;
        }
        public Task MarkAsFailedAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

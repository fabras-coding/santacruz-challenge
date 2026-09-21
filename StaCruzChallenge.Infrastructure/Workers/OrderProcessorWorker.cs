using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using StaCruzChallenge.Domain.Entities;
using StaCruzChallenge.Domain.Repositories;
using System.Text.Json;
using StaCruzChallenge.Application.Interfaces.ExternalServices;
using StaCruzChallenge.Domain.Enums;
namespace StaCruzChallenge.Infrastructure.Workers
{
    public class OrderProcessorWorker : BackgroundService
    {

        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OrderProcessorWorker> _logger;

        private IConnection? _connection;
        private IChannel? _channel;

        public OrderProcessorWorker(IServiceScopeFactory serviceScopeFactory, IConfiguration configuration, ILogger<OrderProcessorWorker> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _configuration = configuration;
            _logger = logger;

        }


        //Consume RabbitMQ messages for order processing
        //Call a stub to simulate external service processing
        //Use the FakeCallsWillSucceed and FakeCallDurationInMilliseconds settings to control the behavior of the stub
        // Get the following repositories from the service scope:
        // - OrderRepository -> just successfully update order status
        // - OutboxRepository -> just successfully update outbox status
        // - OrderProcessingAttemptRepository -> every processing attempt should be recorded

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMq:Host"]!,
                Port = int.Parse(_configuration["RabbitMq:Port"]!),
                UserName = _configuration["RabbitMq:User"]!,
                Password = _configuration["RabbitMq:Password"]!
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var queue = _configuration["RabbitMq:Queue"]!;
            var consumer = new AsyncEventingBasicConsumer(_channel!);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                var payload = Encoding.UTF8.GetString(ea.Body.ToArray());

                try
                {
                    await ProcessMessageAsync(payload, stoppingToken);
                    await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process order message.");
                    await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, stoppingToken);
                }
            };

            await _channel!.BasicConsumeAsync(queue, autoAck: false, consumer, stoppingToken);

            // Keep the service alive; the consumer above handles messages as events.
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task ProcessMessageAsync(string payload, CancellationToken cancellationToken)
        {
            var order = JsonSerializer.Deserialize<Order>(payload)
                ?? throw new InvalidOperationException("Could not deserialize order payload.");

            using var scope = _serviceScopeFactory.CreateScope();
            var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var attemptRepository = scope.ServiceProvider.GetRequiredService<IOrderProcessingAttemptsRepository>();
            var outboxOrderRepository = scope.ServiceProvider.GetRequiredService<IOutboxOrderRepository>();
            var externalCaller = scope.ServiceProvider.GetRequiredService<IExternalOrderCaller>();

            var previousAttempts = await attemptRepository.GetByOrderIdAsync(order.Id, cancellationToken);

            var attempt = new OrderProcessingAttempts
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                StartedAt = DateTime.UtcNow,
                AttemptNumber = previousAttempts.Count + 1
            };

            try
            {
                var success = await externalCaller.ProcessOrderAsync(order.Id, cancellationToken);

                attempt.EndedAt = DateTime.UtcNow;
                attempt.Success = success;

                await orderRepository.UpdateStatusAsync(
                    order.Id,
                    (success ? OrderStatus.Processed : OrderStatus.Failed).ToString(),
                    cancellationToken);



            }
            catch (Exception ex)
            {
                attempt.EndedAt = DateTime.UtcNow;
                attempt.Success = false;
                attempt.ErrorMessage = ex.Message;

                await orderRepository.UpdateStatusAsync(order.Id, OrderStatus.Failed.ToString(), cancellationToken);
            }
            finally
            {
                await attemptRepository.AddAsync(attempt, cancellationToken);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_channel is not null) await _channel.CloseAsync(cancellationToken: cancellationToken);
            if (_connection is not null) await _connection.CloseAsync(cancellationToken: cancellationToken);
            await base.StopAsync(cancellationToken);
        }
    }


}
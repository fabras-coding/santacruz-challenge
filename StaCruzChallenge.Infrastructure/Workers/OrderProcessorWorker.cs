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
                    var result = await ProcessMessageAsync(payload, stoppingToken);
                    if (result.Item1)
                        {
                            await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
                            _logger.LogInformation("Order message processed successfully.");
                        }
                    else if(!result.Item1 && !result.Item2)
                        {
                            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, stoppingToken);
                            _logger.LogWarning("Order message processing failed, message requeued.");
                        }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process order message.");
                    await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, stoppingToken);
                }
            };

            await _channel!.BasicConsumeAsync(queue, autoAck: false, consumer, stoppingToken);

            _logger.LogInformation("Order processor worker started and consuming messages from queue {Queue}.", queue);
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task<Tuple<bool, bool>> ProcessMessageAsync(string payload, CancellationToken cancellationToken)
        {
            var JsonDocument = JsonSerializer.Deserialize<JsonDocument>(payload)
                ?? throw new InvalidOperationException("Could not deserialize the payload.");

            var orderId = JsonDocument.RootElement.GetProperty("OrderId").GetInt64();
            var outboxMessageId = JsonDocument.RootElement.GetProperty("OutboxMessageId").GetGuid();

            using var scope = _serviceScopeFactory.CreateScope();
            var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var attemptRepository = scope.ServiceProvider.GetRequiredService<IOrderProcessingAttemptsRepository>();
            var outboxOrderRepository = scope.ServiceProvider.GetRequiredService<IOutboxOrderRepository>();
            var externalCaller = scope.ServiceProvider.GetRequiredService<IExternalOrderCaller>();

            var previousAttempts = await attemptRepository.GetByOrderIdAsync(orderId, cancellationToken);
            var maxRetryAttempts = _configuration.GetValue<int>("ProcessingAttempt:MaxRetryAttempts");

            if(previousAttempts.Any(att => att.Success))
            {
                _logger.LogInformation("Order {OrderId} has already been successfully processed.", orderId);
                return Tuple.Create(true, false);
            }

            if(previousAttempts.Count >= maxRetryAttempts)
            {
                _logger.LogWarning("Order {OrderId} has reached the maximum number of retry attempts.", orderId);
                return Tuple.Create(false, true);
            }


            var attempt = new OrderProcessingAttempts
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                StartedAt = DateTime.Now,
                AttemptNumber = previousAttempts.Count + 1
            };

            try
            {
                var success = await externalCaller.ProcessOrderAsync(orderId, cancellationToken);

                attempt.EndedAt = DateTime.Now;
                attempt.Success = success;
                attempt.ErrorMessage = success ? null : "Configuration set to fail intentionally.";

                if (success)
                {
                    await orderRepository.UpdateStatusAsync(
                    orderId, OrderStatus.Processed.ToString(),
                    cancellationToken);

                    await outboxOrderRepository.MarkAsProcessedAsync(outboxMessageId, cancellationToken);
                    _logger.LogInformation("Order {OutboxMessageId} processed successfully.", outboxMessageId);
                }


                return Tuple.Create(success, false);
            }
            catch (Exception ex)
            {
                attempt.EndedAt = DateTime.Now;
                attempt.Success = false;
                attempt.ErrorMessage = ex.Message;

                await orderRepository.UpdateStatusAsync(orderId, OrderStatus.Failed.ToString(), cancellationToken);
                _logger.LogError(ex, "Failed to process order {OrderId} on attempt {AttemptNumber}.", orderId, attempt.AttemptNumber);
                return Tuple.Create(false, false);
            }
            finally
            {
                await attemptRepository.AddAsync(attempt, cancellationToken);
                _logger.LogInformation("Recorded attempt {AttemptNumber} for order {OrderId} with success status {Success}.", attempt.AttemptNumber, attempt.OrderId, attempt.Success);
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
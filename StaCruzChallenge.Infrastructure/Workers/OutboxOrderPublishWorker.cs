using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StaCruzChallenge.Application.Interfaces.Messaging;
using System.Threading;
using System.Text.Json;
using StaCruzChallenge.Domain.Entities;
using StaCruzChallenge.Domain.Repositories;

namespace StaCruzChallenge.Infrastructure.Workers
{
    public class OutboxOrderPublishWorker : BackgroundService
    {

        private TimeSpan PoolingInterval;
        private int BatchSize;
        private int ForgottenMessagesIntervalMinutes; 
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OutboxOrderPublishWorker> _logger;

        public OutboxOrderPublishWorker(IServiceScopeFactory serviceScopeFactory, IConfiguration configuration, ILogger<OutboxOrderPublishWorker> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _configuration = configuration;
            _logger = logger;
            PoolingInterval = TimeSpan.FromSeconds(_configuration.GetValue<int>("OutboxOrder:PollingInterval"));
            BatchSize = _configuration.GetValue<int>("OutboxOrder:BatchSize");
            ForgottenMessagesIntervalMinutes = _configuration.GetValue<int>("OutboxOrder:ForgottenMessagesIntervalMinutes");
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Checking for pending outbox order messages...");

                var messages = await GetPendingOutboxMessagesAsync(stoppingToken);
                
                if (messages is not null && messages.Any())
                {

                    using var scope = _serviceScopeFactory.CreateScope();
                    var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
                    var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxOrderRepository>();

                    foreach (var message in messages)
                    {
                        await ProcessMessageAsync(message, eventPublisher, outboxRepository, stoppingToken);
                    }

                }

                await Task.Delay(PoolingInterval, stoppingToken);

            }
        }

        private async Task<IEnumerable<OutboxOrderMessage>> GetPendingOutboxMessagesAsync(CancellationToken stoppingToken)
        {

            using var scope = _serviceScopeFactory.CreateScope();
            var outboxOrderRepository = scope.ServiceProvider.GetRequiredService<IOutboxOrderRepository>();
            return await outboxOrderRepository.GetPendingAsync(BatchSize, ForgottenMessagesIntervalMinutes, stoppingToken);

        }

        private async Task ProcessMessageAsync(OutboxOrderMessage message, IEventPublisher eventPublisher, IOutboxOrderRepository outboxRepository, CancellationToken stoppingToken)
        {

            try
            {
                var rabbitPayload = new
                {
                    OutboxMessageId = message.Id,
                    OrderId = JsonSerializer.Deserialize<JsonElement>(message.Payload).GetProperty("OrderId").GetInt64()
                };

                await eventPublisher.PublishAsync(message.EventType, JsonSerializer.Serialize(rabbitPayload), stoppingToken);
                await outboxRepository.MarkAsEnqueuedAsync(message.Id, stoppingToken);
                _logger.LogInformation("Successfully published outbox message {MessageId} to RabbitMQ", message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox message {MessageId}", message.Id);
                await outboxRepository.MarkAsFailedAsync(message.Id, stoppingToken);
            }

        }

    }
}
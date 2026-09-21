using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using StaCruzChallenge.Application.Interfaces.Messaging;

namespace StaCruzChallenge.Infrastructure.Messaging
{
    public class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
    {

        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMqEventPublisher> _logger;
        private IConnection? _connection;
        private IChannel? _channel;

        public RabbitMqEventPublisher(IConfiguration configuration, ILogger<RabbitMqEventPublisher> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _logger.LogInformation("RabbitMqEventPublisher initialized.");
        }

        private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
        {

            if (_channel is not null)
                return _channel;

            try
            {
                if (_channel is not null)
                    return _channel;

                var factory = new ConnectionFactory
                {
                    HostName = _configuration["RabbitMq:Host"]!,
                    Port = int.Parse(_configuration["RabbitMq:Port"]!),
                    UserName = _configuration["RabbitMq:User"]!,
                    Password = _configuration["RabbitMq:Password"]!
                };

                var exchange = _configuration["RabbitMq:Exchange"]!;
                var queue = _configuration["RabbitMq:Queue"]!;
                var routingKey = _configuration["RabbitMq:RoutingKey"]!;

                _connection = await factory.CreateConnectionAsync(cancellationToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
                
                await _channel.ExchangeDeclareAsync(exchange, ExchangeType.Direct, durable: true, cancellationToken: cancellationToken);
                await _channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
                await _channel.QueueBindAsync(queue, exchange, routingKey: routingKey, cancellationToken: cancellationToken);


                _logger.LogInformation("RabbitMqEventPublisher initialized.");
                return _channel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize RabbitMqEventPublisher.");
                throw;
            }
        }


        public async ValueTask DisposeAsync()
        {
            if (_channel is not null)
                await _channel.DisposeAsync();
            if (_connection is not null)
                await _connection.DisposeAsync();
        }


        public async Task PublishAsync(string eventType, string payload, CancellationToken cancellationToken)
        {
            
            var channel = await GetChannelAsync(cancellationToken);
            var body = Encoding.UTF8.GetBytes(payload);

            var props = new BasicProperties {Persistent = true };

            await channel.BasicPublishAsync(
                exchange: _configuration["RabbitMq:Exchange"]!,
                routingKey: eventType,
                mandatory: true,
                basicProperties: props,
                body: body,
                cancellationToken: cancellationToken
            );
            

        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StaCruzChallenge.Application.Interfaces;
using StaCruzChallenge.Application.Orders;
using StaCruzChallenge.Domain.Entities;
using StaCruzChallenge.Domain.Enums;
using StaCruzChallenge.Domain.Repositories;

namespace StaCruzChallenge.Application.Services
{
    public class OrderService : IOrderService
    {

        private readonly IOrderRepository _orderRepository;
        private readonly IProductService _productService;
        private readonly ILogger<OrderService> _logger;

        public OrderService(IOrderRepository orderRepository, IProductService productService, ILogger<OrderService> logger)
        {
            _orderRepository = orderRepository;
            _productService = productService;
            _logger = logger;
        }

        public async Task<OrderCreatedResponseDto> CreateOrderAsync(CreateOrderDto order, CancellationToken cancellationToken)
        {

            var products = await _productService.GetAllAsync(cancellationToken);
            var validProducts = products.Where(p => order.Items!.Any(i => i.ProductId == p.Id)).ToArray();

            var excludedProducts = order.Items!.Where(i => validProducts.All(p => p.Id != i.ProductId)).ToArray();
            if (excludedProducts.Any())
                _logger.LogWarning("The following products are not valid and will be excluded from the order: {ExcludedProducts}", excludedProducts.Select(i => i.ProductId));


            var orderEntity = new Order()
            {
                Items = order.Items!.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitValue = validProducts.First(p => p.Id == i.ProductId).Price,
                    TotalAmount = i.Quantity * validProducts.First(p => p.Id == i.ProductId).Price
                }).ToArray(),
                TotalAmount = order.Items!.Where(i => validProducts.All(p => p.Id != i.ProductId) == false)
                    .Sum(i => i.Quantity * validProducts.First(p => p.Id == i.ProductId).Price)
            };

            var outboxMessage = new OutboxOrderMessage
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.Now,
                EventType = "order.created",
                Status = OrderStatus.Pending.ToString()

            }; 

            var orderId = await _orderRepository.CreateAsync(orderEntity, outboxMessage, cancellationToken);
            _logger.LogInformation("Order created successfully with {ItemCount} items.", orderEntity.Items.Count);

            return new OrderCreatedResponseDto
            {
                OrderId = orderId,
                Status = "Pending"
            };
        }

        public async Task<IEnumerable<GetOrderDto>> GetAllPaginatedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
        {
            if(pageNumber <= 0)
                throw new ArgumentException("Page number must be greater than zero.", nameof(pageNumber));
            if(pageSize <= 0)
                throw new ArgumentException("Page size must be greater than zero.", nameof(pageSize));

            var orders = await _orderRepository.GetAllPaginatedAsync(pageNumber, pageSize, cancellationToken);

            return orders.Select(order => new GetOrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                TotalAmount = order.TotalAmount,
                Status = order.Status!,
                Items = order.Items.Select(i => new GetOrderItemDto
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitValue = i.UnitValue,
                    TotalAmount = i.TotalAmount
                }).ToArray()
            }).ToArray();
        }

        public async Task<GetOrderDto?> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid order ID.", nameof(id));
            
            var order = await _orderRepository.GetByIdAsync(id, cancellationToken);

            if (order == null)
                return null;

            return new GetOrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                TotalAmount = order.TotalAmount,
                Status = order.Status!,
                Items = order.Items.Select(i => new GetOrderItemDto
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitValue = i.UnitValue,
                    TotalAmount = i.TotalAmount
                }).ToArray()
            };

        }
    }
}
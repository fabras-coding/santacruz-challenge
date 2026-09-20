using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StaCruzChallenge.Application.Orders
{
    public record GetOrderDto
    {
        public long Id { get; init; }
        public Guid UserId { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public decimal TotalAmount { get; init; }
        public string? Status { get; init; }
        public GetOrderItemDto[]? Items { get; init; }
    }

    public record GetOrderItemDto
    {
        public Guid ProductId { get; init; }
        public int Quantity { get; init; }
        public decimal UnitValue { get; init; }
        public decimal TotalAmount { get; init; }
    }
}
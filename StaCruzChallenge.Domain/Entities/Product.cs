using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StaCruzChallenge.Domain.Entities
{
    public sealed class Product
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = null!;
        public string? Description { get; init; }
        public decimal Price { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
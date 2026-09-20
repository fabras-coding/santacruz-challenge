using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace StaCruzChallenge.Application.Orders
{
    public record CreateOrderDto
    {
        
        [Required]
        public OrderItemDto[]? Items { get; set; }
        
    }
}
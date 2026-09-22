using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace StaCruzChallenge.Application.Orders
{
    public record OrderItemDto
    {
        [Required]
        public Guid ProductId { get; set; }
        [Required, Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100.")]
        public int Quantity { get; set; }
        
    }
}
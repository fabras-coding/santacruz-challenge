using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StaCruzChallenge.Application.Orders
{
    public class OrderCreatedResponseDto
    {
        public long OrderId {get;set;}
        public string? Status { get; set; }
    }
}
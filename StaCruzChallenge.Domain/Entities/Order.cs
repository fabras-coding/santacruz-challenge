using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StaCruzChallenge.Domain.Entities
{
    public sealed class Order
    {
        
        public long Id { get; set; }
        public Guid UserId {get;set;}
        public DateTime CreatedAt {get;set;} = DateTime.Now;
        public DateTime UpdatedAt {get;set;} = DateTime.Now;
    
        public decimal TotalAmount {get;set;}
        public string? Status {get;set;} = "Pending";
        
        public ICollection<OrderItem>? Items { get; set; }

    }
}
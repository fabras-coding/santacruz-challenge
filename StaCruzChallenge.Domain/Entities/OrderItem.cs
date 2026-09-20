using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StaCruzChallenge.Domain.Entities
{
    public sealed class OrderItem
    {
        
        public Guid Id {get;set;} = Guid.NewGuid();

        public long OrderId {get;set;}
        public Guid ProductId {get;set;}
        public int Quantity {get;set;}
        public decimal UnitValue {get;set;}
        public decimal TotalAmount {get;set;} 

    }
}
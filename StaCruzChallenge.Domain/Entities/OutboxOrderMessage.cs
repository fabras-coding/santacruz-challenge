using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StaCruzChallenge.Domain.Entities
{
    public class OutboxOrderMessage
    {
        public Guid Id { get; set; }
        public string EventType { get; set; }
        public string Payload { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string Status {get;set;}
        
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StaCruzChallenge.Domain.Entities
{
    public class OrderProcessingAttempts
    {
        public Guid Id { get; set; }
        public long OrderId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public int AttemptNumber { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
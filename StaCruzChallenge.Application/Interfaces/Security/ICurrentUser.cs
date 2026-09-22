using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StaCruzChallenge.Application.Interfaces.Security
{
    public interface ICurrentUser
    {
        public Guid UserId { get; }
    }
}
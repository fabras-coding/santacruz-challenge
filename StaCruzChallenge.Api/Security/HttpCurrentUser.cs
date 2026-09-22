using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StaCruzChallenge.Application.Interfaces.Security;

namespace StaCruzChallenge.Api.Security
{
    public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
    {
        public Guid UserId { 
            get
            {
                var userIdClaim = httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
                
                if (!Guid.TryParse(userIdClaim, out var userId))
                    throw new UnauthorizedAccessException("The token does not contain a valid subject.");

            return userId;
            }
        }
    }
}
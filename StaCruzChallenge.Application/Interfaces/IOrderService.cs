using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StaCruzChallenge.Application.Orders;

namespace StaCruzChallenge.Application.Interfaces
{
    public interface IOrderService
    {
        Task<OrderCreatedResponseDto> CreateOrderAsync(CreateOrderDto order, CancellationToken cancellationToken);
        Task<GetOrderDto?> GetByIdAsync(long id, CancellationToken cancellationToken);
        Task<IEnumerable<GetOrderDto>> GetAllPaginatedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StaCruzChallenge.Application.Interfaces;
using StaCruzChallenge.Application.Orders;

namespace StaCruzChallenge.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateOrderAsync([FromBody] CreateOrderDto order, CancellationToken cancellationToken)
        {
            var result = await _orderService.CreateOrderAsync(order, cancellationToken);
            return Accepted(result);
        }

        [Authorize]
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetOrderByIdAsync(long id , CancellationToken cancellationToken)
        {
            if (id <= 0)
                return BadRequest("Invalid order ID.");

            var result = await _orderService.GetByIdAsync(id, cancellationToken);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllOrdersAsync(int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
        {
            var result = await _orderService.GetAllPaginatedAsync(pageNumber, pageSize, cancellationToken);
            return Ok(result);
        }

    }
}
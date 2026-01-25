using API.Models;
using Application.DTO.Order;
using Application.Interfaces;
using Asp.Versioning;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.v1
{
    /// <summary>
    /// Orders Controller V1 - Stable version (⚠️ DEPRECATED - Use V2)
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("v{version:apiVersion}/orders")]
    [ApiVersion("1.0", Deprecated = true)]  // 👈 Marcado como deprecated
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        #region GETS
        /// <summary>
        /// Returns all orders registered.
        /// </summary>
        /// <returns>List of Orders</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<OrderResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return Ok(orders);
        }

        /// <summary>
        /// Returns a order by id.
        /// </summary>
        /// <returns>Object Order</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            return Ok(order);
        }
        #endregion

        #region POST
        /// <summary>
        /// Add a order.
        /// </summary>
        /// <returns>Object order added</returns>
        [HttpPost(Name = "Order")]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Add([FromBody] AddOrderRequest orderRequest)
        {
            // getting user_id and user_email from context (provided by token)
            orderRequest.UserId = HttpContext.User?.FindFirst("user_id")?.Value ?? "anonymous"; // getting user_id from context (provided by token)
            orderRequest.Email = HttpContext.User?.FindFirst("user_email")?.Value; 

            var createdOrder = await _orderService.AddOrder(orderRequest);
            return CreatedAtAction(nameof(GetById), new { id = createdOrder.OrderId }, createdOrder);
        }
        #endregion

        #region PATCH
        /// <summary>
        /// Update a order.
        /// </summary>
        /// <returns>Object Order updated</returns>
        [HttpPatch("{id}")]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(int id, [FromQuery] OrderStatus orderStatus)
        {
            var orderRequest = new UpdateOrderRequest()
            {
                OrderId = id,
                UserId = HttpContext.User?.FindFirst("user_id")?.Value ?? "anonymous", // getting user_id from context (provided by token)
                Status = orderStatus
            };

            var updated = await _orderService.UpdateOrder(orderRequest);
            return Ok(updated);
        }
        #endregion

        #region DELETE
        /// <summary>
        /// Delete a order.
        /// </summary>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(int id)
        {
            await _orderService.DeleteOrderAsync(id);
            return NoContent();
        }
        #endregion
    }
}

using API.Helpers;
using API.Models;
using Application.DTO.Common;
using Application.DTO.Order;
using Application.Interfaces;
using Asp.Versioning;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.v2
{
    /// <summary>
    /// Orders Controller V2 - Enhanced version with HATEOAS support
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("v{version:apiVersion}/orders")]
    [ApiVersion("2.0")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        #region GETS
        /// <summary>
        /// Returns paginated orders with metadata and HATEOAS links (V2).
        /// </summary>
        /// <param name="paginationParams">Pagination parameters</param>
        /// <returns>Paginated list of orders with navigation links</returns>
        /// <remarks>
        /// V2 Improvements:
        /// - Paginação como padrão (não precisa /all)
        /// - Headers HTTP com metadados
        /// - **HATEOAS links** para navegação (self, next, prev, first, last)
        /// - Melhor performance
        /// 
        /// Example: GET /v2/orders?page=2&amp;pageSize=20
        /// 
        /// Response includes "_links" with navigation URLs:
        /// - self: Current page
        /// - next: Next page (if exists)
        /// - previous: Previous page (if exists)
        /// - first: First page
        /// - last: Last page
        /// </remarks>
        [HttpGet(Name = "GetOrders")]
        [ProducesResponseType(typeof(PagedResponse<OrderResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPaginated([FromQuery] PaginationParameters paginationParams)
        {
            var pagedOrders = await _orderService.GetOrdersPaginatedAsync(paginationParams);
            
            // V2: Headers mais completos (usando indexer para evitar duplicação)
            Response.Headers["X-Pagination-CurrentPage"] = pagedOrders.CurrentPage.ToString();
            Response.Headers["X-Pagination-PageSize"] = pagedOrders.PageSize.ToString();
            Response.Headers["X-Pagination-TotalCount"] = pagedOrders.TotalCount.ToString();
            Response.Headers["X-Pagination-TotalPages"] = pagedOrders.TotalPages.ToString();
            Response.Headers["X-Pagination-HasNext"] = pagedOrders.HasNext.ToString().ToLower();
            Response.Headers["X-Pagination-HasPrevious"] = pagedOrders.HasPrevious.ToString().ToLower();
            
            // HATEOAS: Adiciona links de navegação em cada pedido
            HateoasHelper.AddLinksToOrders(pagedOrders.Data, Url, "2.0");
            
            // HATEOAS: Adiciona links de paginação na resposta
            pagedOrders.Links = HateoasHelper.CreatePaginationLinks(Url, pagedOrders, "GetOrders", "2.0");
            
            return Ok(pagedOrders);
        }

        /// <summary>
        /// Returns an order by id with HATEOAS links (V2).
        /// </summary>
        /// <param name="id">Order ID</param>
        /// <returns>Order with available action links</returns>
        /// <remarks>
        /// V2 Enhancement: Includes HATEOAS links for:
        /// - self: View this order
        /// - update: Update order status
        /// - delete: Delete order
        /// - all: List all orders
        /// - Conditional links based on order status (pay, release, etc)
        /// </remarks>
        [HttpGet("{id}", Name = "GetOrderById")]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            
            // HATEOAS: Adiciona links de ações disponíveis
            HateoasHelper.AddLinksToOrder(order, Url, "2");
            
            return Ok(order);
        }
        #endregion

        #region POST
        /// <summary>
        /// Add an order (V2 - Enhanced).
        /// </summary>
        /// <returns>Object order added</returns>
        [HttpPost(Name = "OrderV2")]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Add([FromBody] AddOrderRequest orderRequest)
        {
            orderRequest.UserId = HttpContext.User?.FindFirst("user_id")?.Value ?? "anonymous"; // getting user_id from context (provided by token)
            orderRequest.Email = HttpContext.User?.FindFirst("user_email")?.Value; 

            var createdOrder = await _orderService.AddOrder(orderRequest);
            return CreatedAtAction(nameof(GetById), new { id = createdOrder.OrderId, version = "2.0" }, createdOrder);
        }
        #endregion

        #region PATCH
        /// <summary>
        /// Update order status with HATEOAS (V2).
        /// </summary>
        /// <param name="id">Order ID</param>
        /// <param name="orderStatus">New order status</param>
        /// <returns>Updated order with action links</returns>
        /// <remarks>
        /// This endpoint is idempotent - calling it multiple times with the same status 
        /// will always produce the same result after the first successful call.
        /// 
        /// V2 Enhancement:
        /// - **HATEOAS links** showing next available actions based on current status
        /// - Same signature as V1 for compatibility
        /// </remarks>
        [HttpPatch("{id}", Name = "UpdateOrderStatus")]
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
                UserId = HttpContext.User?.FindFirst("user_id")?.Value ?? "anonymous",
                Status = orderStatus
            };

            var updated = await _orderService.UpdateOrder(orderRequest);
            
            // HATEOAS: Adiciona links mostrando próximas ações disponíveis
            HateoasHelper.AddLinksToOrder(updated, Url, "2");
            
            return Ok(updated);
        }
        #endregion

        #region DELETE
        /// <summary>
        /// Delete an order (V2 - Same as V1).
        /// </summary>
        /// <returns>No content</returns>
        [HttpDelete("{id}", Name = "DeleteOrder")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
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

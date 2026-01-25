using Application.DTO.Common;
using Application.DTO.Order;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Helpers
{
    /// <summary>
    /// Helper para gerar links HATEOAS consistentes.
    /// 
    /// ?? OBJETIVO:
    /// Centralizar a lógica de criação de links HATEOAS,
    /// garantindo URLs corretas e consistentes.
    /// 
    /// ?? USO:
    /// var links = HateoasHelper.CreateOrderLinks(urlHelper, orderId, version);
    /// orderResponse.Links = links;
    /// </summary>
    public static class HateoasHelper
    {
        /// <summary>
        /// Cria links HATEOAS para um pedido específico.
        /// </summary>
        /// <param name="urlHelper">Helper para gerar URLs</param>
        /// <param name="orderId">ID do pedido</param>
        /// <param name="version">Versão da API (ex: "2.0")</param>
        /// <param name="status">Status atual do pedido (para links condicionais)</param>
        /// <returns>Lista de links HATEOAS</returns>
        public static List<Link> CreateOrderLinks(IUrlHelper urlHelper, int orderId, string version, OrderStatus? status = null)
        {
            var links = new List<Link>
            {
                // Self - Link para o próprio recurso
                new Link(
                    href: urlHelper.Link("GetOrderById", new { id = orderId, version }) ?? string.Empty,
                    rel: "self",
                    method: "GET"
                ),
                
                // Update - Link para atualizar o status (apenas se não estiver finalizado)
                new Link(
                    href: urlHelper.Link("UpdateOrderStatus", new { id = orderId, version }) ?? string.Empty,
                    rel: "update",
                    method: "PATCH"
                ),
                
                // Delete - Link para deletar (apenas se não estiver pago/liberado)
                new Link(
                    href: urlHelper.Link("DeleteOrder", new { id = orderId, version }) ?? string.Empty,
                    rel: "delete",
                    method: "DELETE"
                ),
                
                // All - Link para lista de todos os pedidos
                new Link(
                    href: urlHelper.Link("GetOrders", new { version }) ?? string.Empty,
                    rel: "all",
                    method: "GET"
                )
            };

            // Links condicionais baseados no status
            if (status == OrderStatus.PendingPayment)
            {
                links.Add(new Link(
                    href: urlHelper.Link("UpdateOrderStatus", new { id = orderId, version }) ?? string.Empty,
                    rel: "pay",
                    method: "PATCH"
                ));
            }

            if (status == OrderStatus.Paid)
            {
                links.Add(new Link(
                    href: urlHelper.Link("UpdateOrderStatus", new { id = orderId, version }) ?? string.Empty,
                    rel: "release",
                    method: "PATCH"
                ));
            }

            return links;
        }

        /// <summary>
        /// Cria links HATEOAS para paginação.
        /// </summary>
        public static List<Link> CreatePaginationLinks<T>(
            IUrlHelper urlHelper,
            PagedResponse<T> pagedResponse,
            string routeName,
            string version)
        {
            var links = new List<Link>
            {
                // Self - Página atual
                new Link(
                    href: urlHelper.Link(routeName, new { 
                        version, 
                        page = pagedResponse.CurrentPage, 
                        pageSize = pagedResponse.PageSize 
                    }) ?? string.Empty,
                    rel: "self",
                    method: "GET"
                ),
                
                // First - Primeira página
                new Link(
                    href: urlHelper.Link(routeName, new { 
                        version, 
                        page = 1, 
                        pageSize = pagedResponse.PageSize 
                    }) ?? string.Empty,
                    rel: "first",
                    method: "GET"
                ),
                
                // Last - Última página
                new Link(
                    href: urlHelper.Link(routeName, new { 
                        version, 
                        page = pagedResponse.TotalPages, 
                        pageSize = pagedResponse.PageSize 
                    }) ?? string.Empty,
                    rel: "last",
                    method: "GET"
                )
            };

            // Previous - Página anterior (se existir)
            if (pagedResponse.HasPrevious)
            {
                links.Add(new Link(
                    href: urlHelper.Link(routeName, new { 
                        version, 
                        page = pagedResponse.CurrentPage - 1, 
                        pageSize = pagedResponse.PageSize 
                    }) ?? string.Empty,
                    rel: "previous",
                    method: "GET"
                ));
            }

            // Next - Próxima página (se existir)
            if (pagedResponse.HasNext)
            {
                links.Add(new Link(
                    href: urlHelper.Link(routeName, new { 
                        version, 
                        page = pagedResponse.CurrentPage + 1, 
                        pageSize = pagedResponse.PageSize 
                    }) ?? string.Empty,
                    rel: "next",
                    method: "GET"
                ));
            }

            return links;
        }

        /// <summary>
        /// Adiciona links HATEOAS a um pedido.
        /// </summary>
        public static void AddLinksToOrder(OrderResponse order, IUrlHelper urlHelper, string version)
        {
            order.Links = CreateOrderLinks(urlHelper, order.OrderId, version, order.Status);
        }

        /// <summary>
        /// Adiciona links HATEOAS a uma coleção de pedidos.
        /// </summary>
        public static void AddLinksToOrders(IEnumerable<OrderResponse> orders, IUrlHelper urlHelper, string version)
        {
            foreach (var order in orders)
            {
                AddLinksToOrder(order, urlHelper, version);
            }
        }

        /// <summary>
        /// Gera links HATEOAS para Health Check endpoints.
        /// </summary>
        /// <param name="httpContext">HTTP Context para gerar URLs</param>
        /// <param name="version">Versão da API</param>
        /// <returns>Lista de links HATEOAS para health endpoints</returns>
        public static List<Link> GenerateHealthLinks(HttpContext httpContext, string version)
        {
            var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

            var links = new List<Link>
            {
                // Self - Health check completo
                new Link(
                    href: $"{baseUrl}/v{version}/health",
                    rel: "self",
                    method: "GET"
                ),
                
                // Orders - Link para endpoints principais
                new Link(
                    href: $"{baseUrl}/api/v{version}/orders",
                    rel: "orders",
                    method: "GET"
                ),

                // Swagger Documentation
                new Link(
                    href: $"{baseUrl}/swagger/index.html",
                    rel: "documentation",
                    method: "GET"
                )
            };

            return links;
        }
    }
}



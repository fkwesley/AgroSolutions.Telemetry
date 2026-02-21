using Application.DTO.Common;
using Application.DTO.FieldMeasurement;
using Application.DTO.Health;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Helpers
{
    /// <summary>
    /// Helper para gerar links HATEOAS consistentes para a API de Telemetria.
    /// 
    /// 🎯 OBJETIVO:
    /// Centralizar a lógica de criação de links HATEOAS,
    /// garantindo URLs corretas e navegabilidade da API (Level 3 Richardson Maturity Model).
    /// 
    /// 📘 USO:
    /// var links = HateoasHelper.CreateFieldMeasurementLinks(urlHelper, measurementId, version);
    /// response.Links = links;
    /// </summary>
    public static class HateoasHelper
    {
        /// <summary>
        /// Cria links HATEOAS para uma medição específica.
        /// </summary>
        /// <param name="urlHelper">Helper para gerar URLs</param>
        /// <param name="measurementId">ID da medição</param>
        /// <param name="version">Versão da API (ex: "1.0")</param>
        /// <returns>Lista de links HATEOAS</returns>
        public static List<Link> CreateFieldMeasurementLinks(IUrlHelper urlHelper, Guid measurementId, string version)
        {
            return new List<Link>
            {
                // Self - Link para o próprio recurso
                new Link(
                    href: urlHelper.Link("GetFieldMeasurementById", new { id = measurementId, version }) ?? string.Empty,
                    rel: "self",
                    method: "GET"
                ),
                
                // Collection - Link para a coleção completa
                new Link(
                    href: urlHelper.Link("GetFieldMeasurementsPaginated", new { version }) ?? string.Empty,
                    rel: "collection",
                    method: "GET"
                )
            };
        }

        /// <summary>
        /// Cria links HATEOAS para uma coleção de medições de um campo.
        /// </summary>
        /// <param name="urlHelper">Helper para gerar URLs</param>
        /// <param name="fieldId">ID do campo</param>
        /// <param name="version">Versão da API</param>
        /// <returns>Lista de links HATEOAS</returns>
        public static List<Link> CreateFieldMeasurementCollectionLinks(
            IUrlHelper urlHelper, 
            Guid fieldId, 
            string version)
        {
            return new List<Link>
            {
                // Self - Link para medições deste campo
                new Link(
                    href: urlHelper.Link("GetMeasurementsByFieldId", new { fieldId, version }) ?? string.Empty,
                    rel: "self",
                    method: "GET"
                ),
                
                // Create - Link para criar nova medição
                new Link(
                    href: urlHelper.Link("CreateFieldMeasurement", new { version }) ?? string.Empty,
                    rel: "create",
                    method: "POST"
                )
            };
        }

        /// <summary>
        /// Cria links de paginação HATEOAS.
        /// </summary>
        /// <param name="urlHelper">Helper para gerar URLs</param>
        /// <param name="currentPage">Página atual</param>
        /// <param name="pageSize">Tamanho da página</param>
        /// <param name="totalPages">Total de páginas</param>
        /// <param name="version">Versão da API</param>
        /// <returns>Lista de links de paginação</returns>
        public static List<Link> CreatePaginationLinks(
            IUrlHelper urlHelper,
            int currentPage,
            int pageSize,
            int totalPages,
            string version)
        {
            var links = new List<Link>
            {
                // Self
                new Link(
                    href: urlHelper.Link("GetFieldMeasurementsPaginated", new { page = currentPage, pageSize, version }) ?? string.Empty,
                    rel: "self",
                    method: "GET"
                )
            };

            // First
            if (currentPage > 1)
            {
                links.Add(new Link(
                    href: urlHelper.Link("GetFieldMeasurementsPaginated", new { page = 1, pageSize, version }) ?? string.Empty,
                    rel: "first",
                    method: "GET"
                ));
            }

            // Previous
            if (currentPage > 1)
            {
                links.Add(new Link(
                    href: urlHelper.Link("GetFieldMeasurementsPaginated", new { page = currentPage - 1, pageSize, version }) ?? string.Empty,
                    rel: "previous",
                    method: "GET"
                ));
            }

            // Next
            if (currentPage < totalPages)
            {
                links.Add(new Link(
                    href: urlHelper.Link("GetFieldMeasurementsPaginated", new { page = currentPage + 1, pageSize, version }) ?? string.Empty,
                    rel: "next",
                    method: "GET"
                ));
            }

            // Last
            if (currentPage < totalPages)
            {
                links.Add(new Link(
                    href: urlHelper.Link("GetFieldMeasurementsPaginated", new { page = totalPages, pageSize, version }) ?? string.Empty,
                    rel: "last",
                    method: "GET"
                ));
            }

            return links;
        }

        /// <summary>
        /// Adiciona links HATEOAS a uma única medição.
        /// </summary>
        public static void AddLinksToMeasurement(
            FieldMeasurementResponse measurement, 
            IUrlHelper urlHelper, 
            string version)
        {
            measurement.Links = CreateFieldMeasurementLinks(urlHelper, measurement.Id, version);
        }

        /// <summary>
        /// Adiciona links HATEOAS a uma coleção de medições.
        /// </summary>
        public static void AddLinksToMeasurements(
            IEnumerable<FieldMeasurementResponse> measurements, 
            IUrlHelper urlHelper, 
            string version)
        {
            foreach (var measurement in measurements)
            {
                AddLinksToMeasurement(measurement, urlHelper, version);
            }
        }

        /// <summary>
        /// Gera links HATEOAS para o Health Check.
        /// </summary>
        public static List<Link> GenerateHealthLinks(HttpContext httpContext, string version)
        {
            var request = httpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";

            return new List<Link>
            {
                new Link(
                    href: $"{baseUrl}/v{version}/health",
                    rel: "self",
                    method: "GET"
                ),
                new Link(
                    href: $"{baseUrl}/v{version}/field-measurements",
                    rel: "measurements",
                    method: "GET"
                ),
                new Link(
                    href: $"{baseUrl}/swagger",
                    rel: "api-docs",
                    method: "GET"
                )
            };
        }
    }
}

using API.Helpers;
using API.Models;
using Application.DTO.Common;
using Application.DTO.FieldMeasurement;
using Application.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.v1
{
    /// <summary>
    /// Controller para gerenciamento de medições de campo (telemetria de sensores).
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("v{version:apiVersion}/field-measurements")]
    [ApiVersion("1.0")]
    public class FieldMeasurementsController : ControllerBase
    {
        private readonly IFieldMeasurementService _service;
        private readonly ILogger<FieldMeasurementsController> _logger;

        public FieldMeasurementsController(
            IFieldMeasurementService service,
            ILogger<FieldMeasurementsController> logger)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region GET
        /// <summary>
        /// Retorna uma medição específica por ID.
        /// </summary>
        /// <param name="id">ID da medição</param>
        /// <returns>Objeto FieldMeasurementResponse com links HATEOAS</returns>
        [HttpGet("{id}", Name = "GetFieldMeasurementById")]
        [ProducesResponseType(typeof(FieldMeasurementResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var measurement = await _service.GetMeasurementByIdAsync(id);

            // Add HATEOAS links
            HateoasHelper.AddLinksToMeasurement(measurement, Url, "1.0");

            return Ok(measurement);
        }

        /// <summary>
        /// Retorna todas as medições de um campo específico.
        /// </summary>
        /// <param name="fieldId">ID do campo</param>
        /// <returns>Lista de medições</returns>
        [HttpGet("field/{fieldId}")]
        [ProducesResponseType(typeof(IEnumerable<FieldMeasurementResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetByFieldId(Guid fieldId)
        {
            var measurements = await _service.GetMeasurementsByFieldIdAsync(fieldId);
            return Ok(measurements);
        }

        /// <summary>
        /// Retorna medições paginadas.
        /// </summary>
        /// <param name="page">Número da página (padrão: 1)</param>
        /// <param name="pageSize">Tamanho da página (padrão: 10)</param>
        /// <returns>Resposta paginada com medições</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<FieldMeasurementResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPaginated([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var paginationParams = new PaginationParameters
            {
                Page = page,
                PageSize = pageSize
            };

            var result = await _service.GetMeasurementsPaginatedAsync(paginationParams);
            return Ok(result);
        }
        #endregion

        #region POST
        /// <summary>
        /// Adiciona uma nova medição de telemetria de campo.
        /// Verifica automaticamente condições de alerta (ex: seca prolongada).
        /// </summary>
        /// <param name="request">Dados da medição</param>
        /// <returns>Medição criada com links HATEOAS</returns>
        [HttpPost(Name = "CreateFieldMeasurement")]
        [ProducesResponseType(typeof(FieldMeasurementResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Add([FromBody] AddFieldMeasurementRequest request)
        {
            // getting user_id and user_email from context (provided by token)
            request.UserId = HttpContext.User?.FindFirst("user_id")?.Value ?? "anonymous"; // getting user_id from context (provided by token)

            var createdMeasurement = await _service.AddMeasurementAsync(request);

            // Add HATEOAS links
            HateoasHelper.AddLinksToMeasurement(createdMeasurement, Url, "1.0");

            return CreatedAtAction(
                nameof(GetById), 
                new { id = createdMeasurement.Id }, 
                createdMeasurement);
        }
        #endregion
    }
}

using Application.DTO.FieldMeasurement;
using Domain.Entities;

namespace Application.Mappings
{
    /// <summary>
    /// Extension methods para mapeamento entre FieldMeasurement e DTOs.
    /// </summary>
    public static class FieldMeasurementMappingExtensions
    {
        /// <summary>
        /// Converte DTO de request para entidade de domínio.
        /// </summary>
        public static FieldMeasurement ToEntity(this AddFieldMeasurementRequest request)
        {
            return new FieldMeasurement(
                fieldId: request.FieldId,
                soilMoisture: request.SoilMoisture,
                airTemperature: request.AirTemperature,
                precipitation: request.Precipitation,
                collectedAt: request.CollectedAt,
                userId: request.UserId
            );
        }

        /// <summary>
        /// Converte entidade de domínio para DTO de resposta.
        /// </summary>
        public static FieldMeasurementResponse ToResponse(this FieldMeasurement entity)
        {
            return new FieldMeasurementResponse
            {
                Id = entity.Id,
                FieldId = entity.FieldId,
                SoilMoisture = entity.SoilMoisture,
                AirTemperature = entity.AirTemperature,
                Precipitation = entity.Precipitation,
                CollectedAt = entity.CollectedAt,
                ReceivedAt = entity.ReceivedAt,
                UserId = entity.UserId
            };
        }
    }
}

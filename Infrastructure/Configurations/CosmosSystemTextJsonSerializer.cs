using Microsoft.Azure.Cosmos;
using System.IO;
using System.Text.Json;

namespace Infrastructure.Configurations
{
    /// <summary>
    /// Custom serializer for CosmosDB that uses System.Text.Json.
    /// This ensures that [JsonPropertyName] attributes are respected,
    /// particularly important for the "id" field mapping.
    /// </summary>
    public class CosmosSystemTextJsonSerializer : CosmosSerializer
    {
        private readonly JsonSerializerOptions _options;

        public CosmosSystemTextJsonSerializer(JsonSerializerOptions options)
        {
            _options = options;
        }

        public override T FromStream<T>(Stream stream)
        {
            using (stream)
            {
                if (stream.CanSeek && stream.Length == 0)
                    return default!;

                return JsonSerializer.Deserialize<T>(stream, _options)!;
            }
        }

        public override Stream ToStream<T>(T input)
        {
            var stream = new MemoryStream();
            JsonSerializer.Serialize(stream, input, _options);
            stream.Position = 0;
            return stream;
        }
    }
}

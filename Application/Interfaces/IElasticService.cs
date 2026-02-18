namespace Application.Interfaces
{
    /// <summary>
    /// Interface for sending data to Elasticsearch.
    /// Provides abstraction for testing and dependency inversion.
    /// </summary>
    public interface IElasticService
    {
        /// <summary>
        /// Sends any object to Elasticsearch with specified index type.
        /// </summary>
        /// <typeparam name="T">Type of object to send</typeparam>
        /// <param name="data">Data to send to Elasticsearch</param>
        /// <param name="indexType">Index type (e.g., "requests", "measurements")</param>
        /// <param name="documentId">Optional document ID. If null, generates a new GUID</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> SendToElasticAsync<T>(T data, string indexType, string? documentId = null) where T : class;
    }
}

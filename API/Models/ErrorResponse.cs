namespace API.Models
{
    public class ErrorResponse
    {
        public required string Message { get; set; }
        public string? Detail { get; set; }
        public Guid? LogId { get; set; } 
    }
}

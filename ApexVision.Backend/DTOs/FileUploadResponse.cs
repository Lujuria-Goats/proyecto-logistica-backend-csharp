namespace ApexVision.Backend.DTOs
{
    public class FileUploadResponse
    {
        public string? Url { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? PublicId { get; set; }
    }
}

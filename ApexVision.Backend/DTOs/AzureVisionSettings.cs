namespace ApexVision.Backend.DTOs
{
    public class AzureVisionSettings
    {
        public string Endpoint { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string[] ValidTags { get; set; } = Array.Empty<string>();
    }
}

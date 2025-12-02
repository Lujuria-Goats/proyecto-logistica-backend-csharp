using System.Threading.Tasks;

namespace ApexVision.Backend.Services
{
    public interface IAiValidationService
    {
        Task<bool> ValidateDeliveryEvidenceAsync(string imageUrl);
    }
}


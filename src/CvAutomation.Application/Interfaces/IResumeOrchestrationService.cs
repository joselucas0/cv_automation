using System.Threading;
using System.Threading.Tasks;
using CvAutomation.Application.DTOs;

namespace CvAutomation.Application.Interfaces;

public interface IResumeOrchestrationService
{
    Task<GenerateResumeResponse> GenerateResumeAsync(GenerateResumeRequest request, CancellationToken ct = default);
}

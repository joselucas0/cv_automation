using System.Threading;
using System.Threading.Tasks;

namespace CvAutomation.Application.Interfaces;

public interface IAiService
{
    Task<string> GenerateContentAsync(string prompt, CancellationToken ct = default);
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default);
    Task<float[][]> GenerateEmbeddingBatchAsync(string[] texts, CancellationToken ct = default);
}

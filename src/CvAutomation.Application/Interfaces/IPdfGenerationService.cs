using System.Threading;
using System.Threading.Tasks;

namespace CvAutomation.Application.Interfaces;

public interface IPdfGenerationService
{
    /// <summary>
    /// Compila o código LaTeX fornecido em um arquivo PDF e retorna os bytes do PDF.
    /// </summary>
    Task<byte[]> GeneratePdfAsync(string latexContent, CancellationToken ct = default);
}

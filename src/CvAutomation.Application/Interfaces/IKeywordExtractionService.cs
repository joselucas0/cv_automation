using System.Threading;
using System.Threading.Tasks;
using CvAutomation.Domain.Models;

namespace CvAutomation.Application.Interfaces;

public interface IKeywordExtractionService
{
    Task<AtsKeywords> ExtractKeywordsAsync(string jobDescription, CancellationToken ct = default);
}

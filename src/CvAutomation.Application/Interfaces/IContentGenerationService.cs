using System.Threading;
using System.Threading.Tasks;
using CvAutomation.Domain.Models;

namespace CvAutomation.Application.Interfaces;

public interface IContentGenerationService
{
    Task<TitleAndAboutContent> GenerateTitleAndAboutAsync(AtsKeywords keywords, string jobTitle, string baseAboutMe, CancellationToken ct = default);
    Task<SkillsContent> GenerateSkillsAsync(AtsKeywords keywords, string skillsContext, CancellationToken ct = default);
    Task<ExperienceContent> GenerateExperienceAsync(AtsKeywords keywords, string experienceName, string baseActuation, string baseItems, string companyContext, bool isLowCoverage = false, string targetStack = "", CancellationToken ct = default);
}

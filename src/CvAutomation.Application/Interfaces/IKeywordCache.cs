using CvAutomation.Domain.Models;

namespace CvAutomation.Application.Interfaces;

public interface IKeywordCache
{
    string GetCacheKey(string jobDescription);
    bool TryGet(string key, out AtsKeywords? keywords);
    void Set(string key, AtsKeywords keywords);
}

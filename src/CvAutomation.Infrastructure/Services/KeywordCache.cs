using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using CvAutomation.Application.Interfaces;
using CvAutomation.Domain.Models;

namespace CvAutomation.Infrastructure.Services;

public class KeywordCache : IKeywordCache
{
    private readonly ConcurrentDictionary<string, AtsKeywords> _cache = new();

    public string GetCacheKey(string jobDescription)
    {
        if (string.IsNullOrWhiteSpace(jobDescription)) return string.Empty;

        var hash = MD5.HashData(Encoding.UTF8.GetBytes(jobDescription.Trim().ToLowerInvariant()));
        return Convert.ToHexString(hash);
    }

    public bool TryGet(string key, out AtsKeywords? keywords)
    {
        return _cache.TryGetValue(key, out keywords);
    }

    public void Set(string key, AtsKeywords keywords)
    {
        if (string.IsNullOrWhiteSpace(key) || keywords == null) return;
        _cache.TryAdd(key, keywords);
    }
}

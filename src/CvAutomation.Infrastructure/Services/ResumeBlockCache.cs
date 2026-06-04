using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CvAutomation.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CvAutomation.Infrastructure.Services;

public class ResumeBlockCache : IResumeBlockCache
{
    private readonly IServiceScopeFactory _scopeFactory;
    private List<CachedBlock>? _cachedBlocks;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public ResumeBlockCache(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<List<CachedBlock>> GetCachedBlocksAsync(CancellationToken ct = default)
    {
        if (_cachedBlocks != null) return _cachedBlocks;

        await _lock.WaitAsync(ct);
        try
        {
            if (_cachedBlocks != null) return _cachedBlocks;

            using var scope = _scopeFactory.CreateScope();
            var dbService = scope.ServiceProvider.GetRequiredService<IResumeDatabaseService>();
            
            var blocks = await dbService.GetAllActiveBlocksAsync(ct);
            _cachedBlocks = blocks.Select(b => new CachedBlock
            {
                Block = b,
                Embedding = string.IsNullOrWhiteSpace(b.EmbeddingJson) || b.EmbeddingJson == "[]"
                    ? Array.Empty<float>()
                    : JsonSerializer.Deserialize<float[]>(b.EmbeddingJson) ?? Array.Empty<float>()
            }).ToList();

            return _cachedBlocks;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Invalidate()
    {
        _lock.Wait();
        try
        {
            _cachedBlocks = null;
        }
        finally
        {
            _lock.Release();
        }
    }
}

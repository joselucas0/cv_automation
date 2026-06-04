using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CvAutomation.Domain.Models;

namespace CvAutomation.Application.Interfaces;

public class CachedBlock
{
    public ResumeBlock Block { get; init; } = null!;
    public float[] Embedding { get; init; } = null!;
}

public interface IResumeBlockCache
{
    Task<List<CachedBlock>> GetCachedBlocksAsync(CancellationToken ct = default);
    void Invalidate();
}

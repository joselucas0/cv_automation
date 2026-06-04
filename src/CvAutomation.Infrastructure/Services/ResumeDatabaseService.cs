using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CvAutomation.Application.Interfaces;
using CvAutomation.Domain.Models;
using CvAutomation.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CvAutomation.Infrastructure.Services;

public class ResumeDatabaseService : IResumeDatabaseService
{
    private readonly AppDbContext _context;

    public ResumeDatabaseService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ResumeBlock>> GetAllActiveBlocksAsync(CancellationToken ct = default)
    {
        return await _context.ResumeBlocks
            .Where(b => b.Active)
            .ToListAsync(ct);
    }

    public async Task SaveGeneratedResumeAsync(GeneratedResume resume, CancellationToken ct = default)
    {
        _context.GeneratedResumes.Add(resume);
        await _context.SaveChangesAsync(ct);
    }
}

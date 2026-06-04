using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CvAutomation.Domain.Models;

namespace CvAutomation.Application.Interfaces;

public interface IResumeDatabaseService
{
    Task<List<ResumeBlock>> GetAllActiveBlocksAsync(CancellationToken ct = default);
    Task SaveGeneratedResumeAsync(GeneratedResume resume, CancellationToken ct = default);
}

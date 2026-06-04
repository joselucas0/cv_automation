using System.Threading;
using System.Threading.Tasks;
using CvAutomation.Application.DTOs;
using CvAutomation.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CvAutomation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResumeController : ControllerBase
{
    private readonly IResumeOrchestrationService _orchestrationService;
    private readonly IResumeBlockCache _blockCache;

    public ResumeController(IResumeOrchestrationService orchestrationService, IResumeBlockCache blockCache)
    {
        _orchestrationService = orchestrationService;
        _blockCache = blockCache;
    }

    [HttpPost("cache/invalidate")]
    public IActionResult InvalidateCache()
    {
        _blockCache.Invalidate();
        return Ok(new { message = "Cache de blocos invalidado com sucesso." });
    }

    [HttpPost("generate")]
    public async Task<ActionResult<GenerateResumeResponse>> Generate(
        [FromBody] GenerateResumeRequest request,
        CancellationToken ct)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.JobDescription))
        {
            return BadRequest("A descrição da vaga não pode estar vazia.");
        }

        if (string.IsNullOrWhiteSpace(request.CompanyName))
        {
            return BadRequest("O nome da empresa não pode estar vazio.");
        }

        try
        {
            var result = await _orchestrationService.GenerateResumeAsync(request, ct);
            return Ok(result);
        }
        catch (System.Exception ex)
        {
            // Retorna um erro detalhado (apropriado para ambiente de desenvolvimento/teste)
            return StatusCode(500, new { error = ex.Message, detail = ex.InnerException?.Message });
        }
    }
}

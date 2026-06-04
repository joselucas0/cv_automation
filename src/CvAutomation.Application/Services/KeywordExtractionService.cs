using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CvAutomation.Application.Interfaces;
using CvAutomation.Application.Prompts;
using CvAutomation.Domain.Models;

namespace CvAutomation.Application.Services;

public class KeywordExtractionService : IKeywordExtractionService
{
    private readonly IAiService _aiService;
    private readonly JsonSerializerOptions _jsonOptions;

    public KeywordExtractionService(IAiService aiService)
    {
        _aiService = aiService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<AtsKeywords> ExtractKeywordsAsync(string jobDescription, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(jobDescription))
            throw new ArgumentException("A descrição da vaga não pode estar vazia.", nameof(jobDescription));

        var prompt = KeywordExtractionPrompt.Template.Replace("{jobDescription}", jobDescription);
        var jsonResponse = await _aiService.GenerateContentAsync(prompt, ct);

        try
        {
            var keywords = JsonSerializer.Deserialize<AtsKeywords>(jsonResponse, _jsonOptions);
            return keywords ?? throw new InvalidOperationException("A IA retornou um JSON nulo ao extrair keywords.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Falha ao desserializar as keywords retornadas pela IA. Resposta bruta: {jsonResponse}", ex);
        }
    }
}

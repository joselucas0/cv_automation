using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CvAutomation.Application.Interfaces;
using CvAutomation.Application.Prompts;
using CvAutomation.Domain.Models;

namespace CvAutomation.Application.Services;

public class ContentGenerationService : IContentGenerationService
{
    private readonly IAiService _aiService;
    private readonly JsonSerializerOptions _jsonOptions;

    public ContentGenerationService(IAiService aiService)
    {
        _aiService = aiService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<TitleAndAboutContent> GenerateTitleAndAboutAsync(AtsKeywords keywords, string jobTitle, string baseAboutMe, CancellationToken ct = default)
    {
        var keywordsJson = JsonSerializer.Serialize(keywords, _jsonOptions);
        var prompt = TitleAndAboutPrompt.Template
            .Replace("{keywordsJson}", keywordsJson)
            .Replace("{jobTitle}", jobTitle)
            .Replace("{baseAboutMe}", baseAboutMe);
        var jsonResponse = await _aiService.GenerateContentAsync(prompt, ct);

        try
        {
            var content = JsonSerializer.Deserialize<TitleAndAboutContent>(jsonResponse, _jsonOptions);
            return content ?? throw new InvalidOperationException("Falha ao desserializar título e sobre mim.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Falha ao desserializar o JSON de título e sobre mim. Resposta bruta: {jsonResponse}", ex);
        }
    }

    public async Task<SkillsContent> GenerateSkillsAsync(AtsKeywords keywords, string skillsContext, CancellationToken ct = default)
    {
        // Prompt compression: only serialize HardSkills and Tools since responsibilities are not needed for Skills
        var compactKeywords = new { keywords.HardSkills, keywords.Tools };
        var keywordsJson = JsonSerializer.Serialize(compactKeywords, _jsonOptions);
        
        var prompt = SkillsPrompt.Template
            .Replace("{keywordsJson}", keywordsJson)
            .Replace("{skillsContext}", skillsContext);
        var jsonResponse = await _aiService.GenerateContentAsync(prompt, ct);

        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;
            
            string skillsLatexValue = string.Empty;
            if (root.TryGetProperty("skillsLatex", out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Array)
                {
                    var list = new System.Collections.Generic.List<string>();
                    foreach (var element in prop.EnumerateArray())
                    {
                        list.Add(element.GetString() ?? string.Empty);
                    }
                    skillsLatexValue = string.Join("\n", list);
                }
                else
                {
                    skillsLatexValue = prop.GetString() ?? string.Empty;
                }
            }
            
            return new SkillsContent { SkillsLatex = skillsLatexValue };
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Falha ao desserializar o JSON de skills. Resposta bruta: {jsonResponse}", ex);
        }
    }

    public async Task<ExperienceContent> GenerateExperienceAsync(
        AtsKeywords keywords,
        string experienceName,
        string baseActuation,
        string baseItems,
        string companyContext,
        bool isLowCoverage = false,
        string targetStack = "",
        CancellationToken ct = default)
    {
        // Prompt compression: exclude JobTitle and KeyResponsibilities from experiences prompt to save tokens
        var compactKeywords = new { keywords.HardSkills, keywords.SoftSkills, keywords.Tools, keywords.Seniority };
        var keywordsJson = JsonSerializer.Serialize(compactKeywords, _jsonOptions);

        var promptTemplate = isLowCoverage ? AbstractionExperiencePrompt.Template : ExperiencePrompt.Template;

        var prompt = promptTemplate
            .Replace("{experienceName}", experienceName)
            .Replace("{baseActuation}", baseActuation)
            .Replace("{baseItems}", baseItems)
            .Replace("{companyContext}", companyContext)
            .Replace("{targetStack}", targetStack)
            .Replace("{keywordsJson}", keywordsJson);

        var jsonResponse = await _aiService.GenerateContentAsync(prompt, ct);

        try
        {
            var content = JsonSerializer.Deserialize<ExperienceContent>(jsonResponse, _jsonOptions);
            return content ?? throw new InvalidOperationException($"Falha ao desserializar experiência: {experienceName}");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Falha ao desserializar o JSON da experiência '{experienceName}'. Resposta bruta: {jsonResponse}", ex);
        }
    }
}

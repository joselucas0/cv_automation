using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CvAutomation.Application.DTOs;
using CvAutomation.Application.Interfaces;
using CvAutomation.Domain.Models;

namespace CvAutomation.Application.Services;

public class ResumeOrchestrationService : IResumeOrchestrationService
{
    private readonly IKeywordExtractionService _keywordExtractionService;
    private readonly IContentGenerationService _contentGenerationService;
    private readonly ILatexTemplateService _latexTemplateService;
    private readonly IPdfGenerationService _pdfGenerationService;
    private readonly IResumeDatabaseService _dbService;
    private readonly IAiService _aiService;
    private readonly IResumeBlockCache _blockCache;
    private readonly IKeywordCache _keywordCache;

    public ResumeOrchestrationService(
        IKeywordExtractionService keywordExtractionService,
        IContentGenerationService contentGenerationService,
        ILatexTemplateService latexTemplateService,
        IPdfGenerationService pdfGenerationService,
        IResumeDatabaseService dbService,
        IAiService aiService,
        IResumeBlockCache blockCache,
        IKeywordCache keywordCache)
    {
        _keywordExtractionService = keywordExtractionService;
        _contentGenerationService = contentGenerationService;
        _latexTemplateService = latexTemplateService;
        _pdfGenerationService = pdfGenerationService;
        _dbService = dbService;
        _aiService = aiService;
        _blockCache = blockCache;
        _keywordCache = keywordCache;
    }

    public async Task<GenerateResumeResponse> GenerateResumeAsync(GenerateResumeRequest request, CancellationToken ct = default)
    {
        // ═══════════════════════════════════════════
        // ETAPA 1 — Extração de Keywords ATS (com cache e concorrência)
        // ═══════════════════════════════════════════
        var cacheKey = _keywordCache.GetCacheKey(request.JobDescription);
        AtsKeywords? keywords = null;
        bool hasCachedKeywords = _keywordCache.TryGet(cacheKey, out keywords);

        Task<AtsKeywords> keywordsTask;
        if (hasCachedKeywords && keywords != null)
        {
            keywordsTask = Task.FromResult(keywords);
        }
        else
        {
            keywordsTask = Task.Run(async () =>
            {
                var k = await _keywordExtractionService.ExtractKeywordsAsync(request.JobDescription, ct);
                _keywordCache.Set(cacheKey, k);
                return k;
            }, ct);
        }

        // ═══════════════════════════════════════════
        // ETAPA 2 — Busca Híbrida / RAG com Cache em Memória
        // ═══════════════════════════════════════════
        float[] queryEmbedding;
        float[] descEmbedding;
        string targetTitle;

        // Se o título já foi fornecido no frontend, podemos adiantar a geração dos embeddings em batch!
        if (!string.IsNullOrWhiteSpace(request.JobTitle))
        {
            targetTitle = request.JobTitle;
            var embeddingsTask = _aiService.GenerateEmbeddingBatchAsync(new[] { targetTitle, request.JobDescription }, ct);

            await Task.WhenAll(keywordsTask, embeddingsTask);

            keywords = keywordsTask.Result;
            var embeddings = embeddingsTask.Result;
            queryEmbedding = embeddings[0];
            descEmbedding = embeddings[1];
        }
        else
        {
            keywords = await keywordsTask;
            targetTitle = keywords.JobTitle;
            var embeddings = await _aiService.GenerateEmbeddingBatchAsync(new[] { targetTitle, request.JobDescription }, ct);
            queryEmbedding = embeddings[0];
            descEmbedding = embeddings[1];
        }

        // 2.2. Recupera todos os blocos de currículo do cache singleton pré-deserializado
        var cachedBlocks = await _blockCache.GetCachedBlocksAsync(ct);
        var allBlocks = cachedBlocks.Select(cb => cb.Block).ToList();

        // 2.3. RAG para "Sobre Mim": Seleciona o melhor resumo baseado no cargo/vaga
        var summaries = cachedBlocks.Where(cb => cb.Block.Type.Equals("summary", StringComparison.OrdinalIgnoreCase)).ToList();
        ResumeBlock? selectedSummaryBlock = null;
        if (summaries.Any())
        {
            var scoredSummaries = summaries.Select(s => 
            {
                var similarity = CosineSimilarity(queryEmbedding, s.Embedding);
                return new { Block = s.Block, Score = similarity };
            })
            .OrderByDescending(x => x.Score)
            .ToList();
            
            selectedSummaryBlock = scoredSummaries.FirstOrDefault()?.Block;
        }
        var baseAboutMe = selectedSummaryBlock?.Content ?? "Sou desenvolvedor de software com experiência no ecossistema de desenvolvimento...";

        // 2.4. Busca de Habilidades: Carrega as habilidades reais do cache
        var skillBlocks = allBlocks.Where(b => b.Type.Equals("skill", StringComparison.OrdinalIgnoreCase)).ToList();
        var skillsContextBuilder = new System.Text.StringBuilder();
        foreach (var skillBlock in skillBlocks)
        {
            var list = JsonSerializer.Deserialize<List<string>>(skillBlock.Content) ?? new List<string>();
            skillsContextBuilder.AppendLine($"- {skillBlock.Title}: {string.Join(", ", list)}");
        }
        var skillsContext = skillsContextBuilder.ToString();

        // 2.5. Seleciona TODAS as 4 experiências profissionais do candidato
        var selectedExperiences = allBlocks
            .Where(b => b.Type.Equals("experience", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(b => b.Priority)
            .ToList();

        // 2.6. RAG para Experiências: Identifica o melhor pool com base na similaridade cosseno (SIMD)
        var poolBlocks = cachedBlocks.Where(cb => cb.Block.Type.Equals("experience_pool", StringComparison.OrdinalIgnoreCase)).ToList();
        ResumeBlock? bestPool = null;
        double coverageScore = 1.0;
        var warnings = new List<string>();
        bool isLowCoverage = false;

        var scoredPools = poolBlocks.Select(p => 
        {
            var similarity = CosineSimilarity(descEmbedding, p.Embedding);
            return new { Block = p.Block, Score = similarity };
        })
        .OrderByDescending(x => x.Score)
        .ToList();

        const double SIMILARITY_THRESHOLD = 0.35;
        var validPools = scoredPools.Where(x => x.Score >= SIMILARITY_THRESHOLD).ToList();
        bestPool = validPools.FirstOrDefault()?.Block;

        if (bestPool != null)
        {
            coverageScore = scoredPools.First().Score;
        }
        else
        {
            isLowCoverage = true;
            coverageScore = scoredPools.FirstOrDefault()?.Score ?? 0.0;
            warnings.Add($"Baixa aderência de stack identificada ({coverageScore:F2}). Adaptando experiências com base em habilidades técnicas e competências transferíveis.");
        }

        // Determina se a vaga é de nível Júnior
        bool isJunior = targetTitle.Contains("junior", StringComparison.OrdinalIgnoreCase) || 
                        targetTitle.Contains("jr", StringComparison.OrdinalIgnoreCase) || 
                        keywords.Seniority.Equals("junior", StringComparison.OrdinalIgnoreCase) || 
                        keywords.Seniority.Equals("jr", StringComparison.OrdinalIgnoreCase);

        // ═══════════════════════════════════════════
        // ETAPA 3 — Fila de Geração com Channel<T>
        // ═══════════════════════════════════════════
        var totalTasks = 2 + selectedExperiences.Count;

        var channel = Channel.CreateBounded<GenerationTask>(new BoundedChannelOptions(totalTasks)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = true
        });

        var results = new ConcurrentDictionary<string, object>();

        // PRODUCER: Enfileira as tarefas dinâmicas do RAG
        var producerTask = Task.Run(async () =>
        {
            // 1. Task para Título e Sobre mim
            await channel.Writer.WriteAsync(new GenerationTask
            {
                TaskId = "title_about",
                ExecuteAsync = async (cToken) => 
                    await _contentGenerationService.GenerateTitleAndAboutAsync(keywords, targetTitle, baseAboutMe, cToken)
            }, ct);

            // 2. Task para Skills
            await channel.Writer.WriteAsync(new GenerationTask
            {
                TaskId = "skills",
                ExecuteAsync = async (cToken) => 
                    await _contentGenerationService.GenerateSkillsAsync(keywords, skillsContext, cToken)
            }, ct);

            // 3. Tasks concorrentes para as 4 experiências selecionadas sob regras de senioridade
            // Ênfases temáticas por empresa para garantir diversidade entre experiências geradas
            var companyEmphasis = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["eleve"] = "ÊNFASE TEMÁTICA OBRIGATÓRIA DESTA EMPRESA: Destaque POCs, entregas inovadoras, acompanhamento de projetos do início ao fim, desenvolvimento hands-on e tomadas de decisões técnicas.",
                ["digix"] = "ÊNFASE TEMÁTICA OBRIGATÓRIA DESTA EMPRESA: Destaque backend robusto, qualidade de código, IA, dados e entregas concretas de valor.",
                ["sigeamb"] = "ÊNFASE TEMÁTICA OBRIGATÓRIA DESTA EMPRESA: Destaque infraestrutura, deploy, DevOps, banco de dados e arquitetura backend.",
                ["decubitocare"] = "ÊNFASE TEMÁTICA OBRIGATÓRIA DESTA EMPRESA: Destaque qualidade de software, testes, domínio hospitalar, implantação e DevOps."
            };

            // Pré-distribui items do pool entre empresas para diversidade mecânica (cada empresa recebe subset disjunto)
            var distributedPoolItems = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            string? resolvedPoolStack = null;

            if (!isJunior)
            {
                string? resolvedPoolContent = null;

                if (isLowCoverage)
                {
                    resolvedPoolStack = scoredPools.Count >= 2 
                        ? $"{scoredPools[0].Block.StackContext} e {scoredPools[1].Block.StackContext}"
                        : (scoredPools.FirstOrDefault()?.Block.StackContext ?? "desenvolvimento de software");

                    if (scoredPools.Count >= 2)
                        resolvedPoolContent = $"{scoredPools[0].Block.Content}\n{scoredPools[1].Block.Content}";
                    else if (scoredPools.Any())
                        resolvedPoolContent = scoredPools[0].Block.Content;
                }
                else if (bestPool != null)
                {
                    resolvedPoolContent = bestPool.Content;
                    resolvedPoolStack = bestPool.StackContext;
                }

                if (resolvedPoolContent != null)
                {
                    var poolLines = resolvedPoolContent
                        .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                        .Where(l => l.TrimStart().StartsWith("*"))
                        .ToList();

                    for (int i = 0; i < poolLines.Count; i++)
                    {
                        var targetExp = selectedExperiences[i % selectedExperiences.Count];
                        var key = targetExp.CompanyKey;
                        if (!distributedPoolItems.ContainsKey(key))
                            distributedPoolItems[key] = new List<string>();
                        distributedPoolItems[key].Add(poolLines[i]);
                    }
                }
            }

            foreach (var exp in selectedExperiences)
            {
                var companyKey = exp.CompanyKey.ToLower();
                string baseActuation = exp.SemanticContent;
                string baseItems = exp.Content;
                string companyContext = string.Empty;

                if (!isJunior) // PLENO / SÊNIOR — Items distribuídos por empresa
                {
                    if (isLowCoverage)
                    {
                        companyContext = $"O candidato NÃO possui experiência direta na stack desejada ({targetTitle}). Porém, possui experiências transferíveis relevantes em {resolvedPoolStack ?? "desenvolvimento de software"}. " +
                                         $"Sua missão é gerar uma descrição de cargo e conquistas focadas em engenharia de software pura, conceitos transferíveis de arquitetura, REST, SOLID e boas práticas, sem citar tecnologias que ele não domina. " +
                                         $"Mantenha todas as experiências reais, mas abstraídas de linguagem.";

                        if (distributedPoolItems.TryGetValue(companyKey, out var poolSubset) && poolSubset.Any())
                        {
                            baseItems = $"{exp.Content}\n{string.Join("\n", poolSubset)}";
                            baseActuation = $"{exp.SemanticContent} Aplicando conceitos transferíveis de engenharia, arquitetura e qualidade técnica em {resolvedPoolStack ?? "desenvolvimento de software"}.";
                        }
                    }
                    else
                    {
                        // Pleno: cada empresa recebe subset disjunto do pool + seus dados factuais reais
                        companyContext = $"Esta empresa deve refletir plenamente a stack principal solicitada na vaga ({resolvedPoolStack ?? "stack da vaga"}) para fortalecer a senioridade Pleno/Sênior. Permissível citar Git e banco de dados.";
                        if (distributedPoolItems.TryGetValue(companyKey, out var poolSubset) && poolSubset.Any())
                        {
                            baseItems = $"{exp.Content}\n{string.Join("\n", poolSubset)}";
                            baseActuation = $"{exp.SemanticContent} Com foco em {resolvedPoolStack ?? "stack da vaga"}.";
                        }
                    }
                }
                else // JÚNIOR
                {
                    if (companyKey == "eleve")
                    {
                        // Eleve Software: Curinga (Wildcard) -> Adapta a qualquer stack
                        if (isLowCoverage)
                        {
                            var combinedStack = scoredPools.Count >= 2 
                                ? $"{scoredPools[0].Block.StackContext} e {scoredPools[1].Block.StackContext}"
                                : (scoredPools.FirstOrDefault()?.Block.StackContext ?? "desenvolvimento de software");

                            companyContext = $"O candidato NÃO possui experiência direta na stack desejada ({targetTitle}). Sendo a Eleve um curinga, foque em abstração de habilidades e competências transferíveis de engenharia de software baseadas em {combinedStack}. Mantenha a experiência factual abstraída de linguagem.";
                            if (scoredPools.Any())
                            {
                                baseItems = scoredPools[0].Block.Content;
                                baseActuation = $"Atuação como desenvolvedor no time de tecnologia, aplicando conceitos universais de programação, arquitetura e entrega contínua em {scoredPools[0].Block.StackContext}.";
                            }
                        }
                        else
                        {
                            companyContext = $"Esta empresa é um curinga (wildcard) na carreira do candidato. Foque 100% nas tecnologias e stack solicitadas na vaga ({bestPool?.StackContext ?? "stack da vaga"}). Permissível citar Git e banco de dados.";
                            if (bestPool != null)
                            {
                                baseItems = bestPool.Content;
                                baseActuation = $"Atuação como desenvolvedor de software no time de tecnologia, com foco em entregas robustas utilizando {bestPool.StackContext}.";
                            }
                        }
                    }
                    else if (companyKey == "digix")
                    {
                        // Digix: C#, .NET, IA, análise de dados
                        companyContext = "Para esta vaga Júnior, a atuação na Digix é focada exclusivamente no ecossistema C#/.NET, IA (Inteligência Artificial), análise de dados, qualidade de código, entregas e automações. NÃO mencione outras stacks concorrentes (ex: Java, React, Node). Permissível citar Git e banco de dados.";
                        
                        var csharpJr = poolBlocks.FirstOrDefault(p => p.Block.Title.Contains("C#") && p.Block.Seniority.Equals("junior"))?.Block.Content ?? string.Empty;
                        var iaJr = poolBlocks.FirstOrDefault(p => p.Block.Title.Contains("IA") && p.Block.Seniority.Equals("junior"))?.Block.Content ?? string.Empty;
                        var dbJr = poolBlocks.FirstOrDefault(p => p.Block.Title.Contains("BANCO") && p.Block.Seniority.Equals("junior"))?.Block.Content ?? string.Empty;
                        
                        baseItems = $"{csharpJr}\n{iaJr}\n{dbJr}";
                        baseActuation = "Atuação no desenvolvimento e manutenção de aplicações backend no ecossistema .NET/C#, participando de soluções voltadas à IA generativa, análise de dados e integrações críticas.";
                    }
                    else if (companyKey == "sigeamb")
                    {
                        // Sigeamb: Especialidade técnica (Backend, deploy, infraestrutura, Docker, Nginx)
                        companyContext = "Para esta vaga Júnior, a atuação no Laboratório de Qualidade da Água (LAQUA/UFMS) é estritamente focada em arquitetura backend, modelagem de banco de dados PostgreSQL, e infraestrutura/deploys utilizando Docker e Nginx como proxy reverso. Mantenha essa especialidade em infraestrutura e backend. Permissível citar Git e banco de dados.";
                    }
                    else if (companyKey == "decubitocare")
                    {
                        // DecubitoCare: Especialidade técnica (QA, testes, arquitetura, implantação, desenvolvimento)
                        companyContext = "Para esta vaga Júnior, a atuação no Hospital Universitário (HU-UFMS) é focada em garantia de qualidade de software, testes automatizados (Cypress, testes de API, funcionais), arquitetura, implantação e desenvolvimento de soluções de saúde. Mantenha essa especialidade em qualidade de software, testes e implantação. Permissível citar Git e banco de dados.";
                    }
                }

                // Injeta ênfase temática para diversificar experiências (não altera performance)
                if (companyEmphasis.TryGetValue(companyKey, out var emphasis))
                {
                    companyContext = string.IsNullOrEmpty(companyContext)
                        ? emphasis
                        : $"{companyContext}\n{emphasis}";
                }

                await channel.Writer.WriteAsync(new GenerationTask
                {
                    TaskId = $"exp_{exp.Id}",
                    ExecuteAsync = async (cToken) => 
                        await _contentGenerationService.GenerateExperienceAsync(
                            keywords, 
                            exp.Company, 
                            baseActuation, 
                            baseItems, 
                            companyContext,
                            isLowCoverage,
                            targetTitle,
                            cToken)
                }, ct);
            }

            channel.Writer.Complete();
        }, ct);

        // CONSUMERS: Processam as tarefas concorrentemente lendo do canal
        var consumerCount = Math.Min(3, totalTasks);
        var consumers = Enumerable.Range(0, consumerCount).Select(async _ =>
        {
            await foreach (var task in channel.Reader.ReadAllAsync(ct))
            {
                var result = await task.ExecuteAsync(ct);
                results.TryAdd(task.TaskId, result);
            }
        });

        await producerTask;
        await Task.WhenAll(consumers);

        // ═══════════════════════════════════════════
        // ETAPA 4 — Consolidação & LaTeX Dinâmico
        // ═══════════════════════════════════════════
        var titleAndAbout = (TitleAndAboutContent)results["title_about"];
        var skills = (SkillsContent)results["skills"];

        var experiencesResult = new Dictionary<string, ExperienceContent>();
        foreach (var exp in selectedExperiences)
        {
            var key = $"exp_{exp.Id}";
            if (results.TryGetValue(key, out var content))
            {
                experiencesResult[key] = (ExperienceContent)content;
            }
        }

        // Renderiza o trecho LaTeX dinâmico das experiências
        var experiencesLatex = new System.Text.StringBuilder();
        foreach (var exp in selectedExperiences)
        {
            var key = $"exp_{exp.Id}";
            if (experiencesResult.TryGetValue(key, out var expContent))
            {
                // Determina o cargo a ser exibido no LaTeX
                string displayTitle = exp.Title;
                if (!isJunior)
                {
                    displayTitle = targetTitle;
                }
                else
                {
                    if (exp.CompanyKey.Equals("eleve", StringComparison.OrdinalIgnoreCase))
                    {
                        displayTitle = targetTitle;
                    }
                    else if (exp.CompanyKey.Equals("digix", StringComparison.OrdinalIgnoreCase))
                    {
                        displayTitle = "Desenvolvedor Backend .NET Júnior";
                    }
                    else if (exp.CompanyKey.Equals("sigeamb", StringComparison.OrdinalIgnoreCase))
                    {
                        displayTitle = "Software Engineer Júnior";
                    }
                    else if (exp.CompanyKey.Equals("decubitocare", StringComparison.OrdinalIgnoreCase))
                    {
                        displayTitle = "QA Engineer | Software Quality Analyst";
                    }
                }

                experiencesLatex.AppendLine($@"\cventry{{{exp.Company}}}{{{exp.Location}}}{{{displayTitle}}}{{{exp.Period}}}");
                experiencesLatex.AppendLine(expContent.ExperienceSummary);
                experiencesLatex.AppendLine(@"\begin{itemize}");
                experiencesLatex.AppendLine(expContent.ExperienceItems);
                experiencesLatex.AppendLine(@"\end{itemize}");
                experiencesLatex.AppendLine();
            }
        }

        var latexContent = _latexTemplateService.GenerateLatex(titleAndAbout, skills, experiencesLatex.ToString());

        // ═══════════════════════════════════════════
        // ETAPA 4.5 — Cálculo de aderência real e injeção de keywords faltantes
        // ═══════════════════════════════════════════
        var targetKeywords = keywords.HardSkills.Concat(keywords.Tools).Distinct().ToList();
        var latexLower = latexContent.ToLower();
        var matchedKeywords = targetKeywords.Where(k => latexLower.Contains(k.ToLower())).ToList();
        var realCoverage = targetKeywords.Count > 0 
            ? (double)matchedKeywords.Count / targetKeywords.Count 
            : 1.0;

        if (realCoverage < 0.80 && targetKeywords.Count > 0)
        {
            // Reúne todas as skills reais do candidato para validação
            var candidateSkills = skillBlocks
                .SelectMany(b => {
                    try { return JsonSerializer.Deserialize<List<string>>(b.Content) ?? new List<string>(); }
                    catch { return new List<string>(); }
                })
                .Select(s => s.ToLower().Trim())
                .ToHashSet();

            var missingKeywords = targetKeywords
                .Where(k => !latexLower.Contains(k.ToLower()))
                .Where(k => candidateSkills.Any(cs => 
                    cs.Contains(k.ToLower()) || k.ToLower().Contains(cs)))
                .ToList();

            if (missingKeywords.Any())
            {
                // Injeta keywords faltantes como categoria extra na seção de Skills
                var escapedKeywords = missingKeywords
                    .Select(k => k.Replace("#", "\\#").Replace("&", "\\&").Replace("%", "\\%"))
                    .ToList();
                var extraSkillLine = $"\n\\item \\textbf{{Complementares:}} {string.Join(", ", escapedKeywords)}";

                // Insere antes do fechamento da seção de Skills
                var skillsEndMarker = "\\end{itemize}\n\n% --- EXPERIÊNCIA";
                if (latexContent.Contains(skillsEndMarker))
                {
                    latexContent = latexContent.Replace(skillsEndMarker, 
                        $"{extraSkillLine}\n\\end{{itemize}}\n\n% --- EXPERIÊNCIA");
                }

                // Recalcula cobertura
                latexLower = latexContent.ToLower();
                matchedKeywords = targetKeywords.Where(k => latexLower.Contains(k.ToLower())).ToList();
                realCoverage = (double)matchedKeywords.Count / targetKeywords.Count;
            }
        }

        coverageScore = realCoverage;

        // ═══════════════════════════════════════════
        // ETAPA 5 — Geração do PDF & Exportações Físicas
        // ═══════════════════════════════════════════
        byte[]? pdfBytes = null;
        string? pdfBase64 = null;
        try
        {
            pdfBytes = await _pdfGenerationService.GeneratePdfAsync(latexContent, ct);
            pdfBase64 = Convert.ToBase64String(pdfBytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao gerar PDF: {ex.Message}");
        }

        // Exportação física das pastas e nomenclatura 'cvJose[Empresa]'
        var exportsPath = @"d:\codas\cv\exports";
        string baseFileName = "cvJose";
        try
        {
            if (!Directory.Exists(exportsPath))
            {
                Directory.CreateDirectory(exportsPath);
            }

            var cleanCompanyName = string.Concat(request.CompanyName.Split(Path.GetInvalidFileNameChars()));
            cleanCompanyName = cleanCompanyName.Replace(" ", ""); // Remove espaços
            baseFileName = $"cvJose{cleanCompanyName}";

            var texFilePath = Path.Combine(exportsPath, $"{baseFileName}.tex");
            var pdfFilePath = Path.Combine(exportsPath, $"{baseFileName}.pdf");

            await File.WriteAllTextAsync(texFilePath, latexContent, ct);
            
            if (pdfBytes != null)
            {
                await File.WriteAllBytesAsync(pdfFilePath, pdfBytes, ct);
            }

            // Grava o histórico de geração no banco de dados SQLite
            var generatedResume = new GeneratedResume
            {
                Title = baseFileName,
                CompanyName = request.CompanyName,
                JobDescription = request.JobDescription,
                JobKeywordsJson = JsonSerializer.Serialize(keywords.HardSkills.Concat(keywords.Tools).Concat(keywords.SoftSkills).Distinct()),
                GeneratedTexPath = texFilePath,
                GeneratedPdfPath = pdfBytes != null ? pdfFilePath : string.Empty,
                UsedBlocksJson = JsonSerializer.Serialize(selectedExperiences.Select(b => b.Id).ToList())
            };
            await _dbService.SaveGeneratedResumeAsync(generatedResume, ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao gravar arquivos físicos ou salvar histórico: {ex.Message}");
        }

        // ═══════════════════════════════════════════
        // ETAPA 6 — Montagem do DTO de Response
        // ═══════════════════════════════════════════
        var extractedKeywords = keywords.HardSkills
            .Concat(keywords.Tools)
            .Concat(keywords.SoftSkills)
            .Distinct()
            .ToList();

        return new GenerateResumeResponse(latexContent, extractedKeywords, pdfBase64, coverageScore, warnings, baseFileName);
    }

    private static double CosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length == 0 || vectorB.Length == 0 || vectorA.Length != vectorB.Length)
            return 0;

        return System.Numerics.Tensors.TensorPrimitives.CosineSimilarity(vectorA.AsSpan(), vectorB.AsSpan());
    }
}

/// <summary>
/// Representa uma tarefa de geração assíncrona a ser enfileirada no Channel.
/// </summary>
public class GenerationTask
{
    public string TaskId { get; init; } = string.Empty;
    public Func<CancellationToken, Task<object>> ExecuteAsync { get; init; } = null!;
}

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CvAutomation.Application.Interfaces;
using CvAutomation.Application.Options;
using Microsoft.Extensions.Options;

namespace CvAutomation.Infrastructure.Services;

public class OpenAiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiSettings _settings;

    public OpenAiService(HttpClient httpClient, IOptions<OpenAiSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task<string> GenerateContentAsync(string prompt, CancellationToken ct = default)
    {
        var apiKey = Environment.GetEnvironmentVariable("CHAT_API_KEY") 
                     ?? Environment.GetEnvironmentVariable("GROQ_API_KEY")
                     ?? (!string.IsNullOrWhiteSpace(_settings.ChatApiKey) ? _settings.ChatApiKey : null)
                     ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
                     ?? _settings.ApiKey;

        var model = Environment.GetEnvironmentVariable("CHAT_MODEL")
                    ?? Environment.GetEnvironmentVariable("GROQ_MODEL")
                    ?? (!string.IsNullOrWhiteSpace(_settings.ChatModel) ? _settings.ChatModel : null)
                    ?? _settings.Model;

        var baseUrl = Environment.GetEnvironmentVariable("CHAT_BASE_URL")
                      ?? Environment.GetEnvironmentVariable("GROQ_BASE_URL")
                      ?? (!string.IsNullOrWhiteSpace(_settings.ChatBaseUrl) ? _settings.ChatBaseUrl : null)
                      ?? _settings.BaseUrl;

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("A API Key de Chat/OpenAI não foi configurada.");

        if (string.IsNullOrWhiteSpace(model))
            throw new InvalidOperationException("O modelo de Chat/OpenAI não foi configurado.");

        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("A URL base da API de Chat/OpenAI não foi configurada.");

        // 1. Monta o corpo da requisição compatível com Chat Completions API do OpenAI
        var requestBody = new
        {
            model = model,
            response_format = new { type = "json_object" }, // Força a resposta a ser JSON puro
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        // 2. Cria a mensagem de requisição manual para configurar o cabeçalho Authorization com Bearer
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/chat/completions");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(requestBody);

        // 3. Executa a requisição
        var response = await _httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Erro na requisição para a API de Chat. Status: {response.StatusCode}. Resposta: {errorContent}");
        }

        // 4. Lê e desserializa o resultado da API da OpenAI (compatível)
        var openAiResponse = await response.Content.ReadFromJsonAsync<OpenAiResponse>(cancellationToken: ct);
        
        var text = openAiResponse?.Choices?[0]?.Message?.Content;

        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("A API de Chat retornou uma resposta sem conteúdo.");

        // 5. Limpa wrappers markdown (```json...) just in case (embora response_format deva evitar isso)
        return CleanMarkdownWrapper(text);
    }

    private static string CleanMarkdownWrapper(string text)
    {
        text = text.Trim();

        if (text.StartsWith("```"))
        {
            var firstNewLineIndex = text.IndexOf('\n');
            if (firstNewLineIndex != -1)
            {
                text = text[(firstNewLineIndex + 1)..];
            }
            else
            {
                text = text[3..];
            }

            if (text.EndsWith("```"))
            {
                text = text[..^3];
            }
        }

        return text.Trim();
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var apiKey = Environment.GetEnvironmentVariable("EMBEDDING_API_KEY") 
                     ?? (!string.IsNullOrWhiteSpace(_settings.EmbeddingApiKey) ? _settings.EmbeddingApiKey : null)
                     ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
                     ?? _settings.ApiKey;

        var model = Environment.GetEnvironmentVariable("EMBEDDING_MODEL")
                    ?? (!string.IsNullOrWhiteSpace(_settings.EmbeddingModel) ? _settings.EmbeddingModel : null)
                    ?? "text-embedding-3-small";

        var baseUrl = Environment.GetEnvironmentVariable("EMBEDDING_BASE_URL")
                      ?? (!string.IsNullOrWhiteSpace(_settings.EmbeddingBaseUrl) ? _settings.EmbeddingBaseUrl : null)
                      ?? _settings.BaseUrl;

        var isLocal = baseUrl.Contains("localhost") || baseUrl.Contains("127.0.0.1") || baseUrl.Contains("::1");
        if (string.IsNullOrWhiteSpace(apiKey) && !isLocal)
            throw new InvalidOperationException("A API Key para Embeddings não foi configurada.");

        var requestBody = new
        {
            input = text,
            model = model
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/embeddings");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        }
        request.Content = JsonContent.Create(requestBody);

        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Erro ao gerar embeddings. Status: {response.StatusCode}. Resposta: {error}");
        }

        var embeddingResponse = await response.Content.ReadFromJsonAsync<OpenAiEmbeddingResponse>(cancellationToken: ct);
        var embedding = embeddingResponse?.Data?[0]?.Embedding;

        if (embedding == null || embedding.Length == 0)
            throw new InvalidOperationException("A API retornou uma resposta sem vetor de embedding.");

        return embedding;
    }

    public async Task<float[][]> GenerateEmbeddingBatchAsync(string[] texts, CancellationToken ct = default)
    {
        var apiKey = Environment.GetEnvironmentVariable("EMBEDDING_API_KEY") 
                     ?? (!string.IsNullOrWhiteSpace(_settings.EmbeddingApiKey) ? _settings.EmbeddingApiKey : null)
                     ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
                     ?? _settings.ApiKey;

        var model = Environment.GetEnvironmentVariable("EMBEDDING_MODEL")
                    ?? (!string.IsNullOrWhiteSpace(_settings.EmbeddingModel) ? _settings.EmbeddingModel : null)
                    ?? "text-embedding-3-small";

        var baseUrl = Environment.GetEnvironmentVariable("EMBEDDING_BASE_URL")
                      ?? (!string.IsNullOrWhiteSpace(_settings.EmbeddingBaseUrl) ? _settings.EmbeddingBaseUrl : null)
                      ?? _settings.BaseUrl;

        var isLocal = baseUrl.Contains("localhost") || baseUrl.Contains("127.0.0.1") || baseUrl.Contains("::1");
        if (string.IsNullOrWhiteSpace(apiKey) && !isLocal)
            throw new InvalidOperationException("A API Key para Embeddings não foi configurada.");

        var requestBody = new
        {
            input = texts,
            model = model
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/embeddings");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        }
        request.Content = JsonContent.Create(requestBody);

        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Erro ao gerar embeddings em lote. Status: {response.StatusCode}. Resposta: {error}");
        }

        var embeddingResponse = await response.Content.ReadFromJsonAsync<OpenAiEmbeddingResponse>(cancellationToken: ct);
        if (embeddingResponse?.Data == null || embeddingResponse.Data.Count == 0)
            throw new InvalidOperationException("A API retornou uma resposta sem vetores de embedding.");

        var results = new float[embeddingResponse.Data.Count][];
        for (int i = 0; i < embeddingResponse.Data.Count; i++)
        {
            results[i] = embeddingResponse.Data[i].Embedding;
        }

        return results;
    }
}

// Classes auxiliares para mapear o JSON de resposta do OpenAI Chat Completions
internal class OpenAiResponse
{
    public List<OpenAiChoice> Choices { get; set; } = [];
}

internal class OpenAiChoice
{
    public OpenAiMessage Message { get; set; } = null!;
}

internal class OpenAiMessage
{
    public string Content { get; set; } = string.Empty;
}

// Classes auxiliares para mapear o JSON de resposta do OpenAI Embeddings
internal class OpenAiEmbeddingResponse
{
    public List<OpenAiEmbeddingData> Data { get; set; } = [];
}

internal class OpenAiEmbeddingData
{
    public float[] Embedding { get; set; } = [];
}

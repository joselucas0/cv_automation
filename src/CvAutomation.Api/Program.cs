using System;
using System.IO;
using CvAutomation.Application.Interfaces;
using CvAutomation.Application.Options;
using CvAutomation.Application.Services;
using CvAutomation.Infrastructure.Data;
using CvAutomation.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;

// Carregar variáveis de ambiente a partir do arquivo .env, se existir
var envPath = Path.Combine(AppContext.BaseDirectory, ".env");
if (!File.Exists(envPath))
{
    var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
    while (currentDir != null && !File.Exists(Path.Combine(currentDir.FullName, ".env")))
    {
        currentDir = currentDir.Parent;
    }
    if (currentDir != null)
    {
        envPath = Path.Combine(currentDir.FullName, ".env");
    }
}

if (File.Exists(envPath))
{
    foreach (var line in File.ReadAllLines(envPath))
    {
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
            continue;

        var parts = line.Split('=', 2);
        if (parts.Length == 2)
        {
            var key = parts[0].Trim();
            var val = parts[1].Trim();
            Environment.SetEnvironmentVariable(key, val);
        }
    }
}

var builder = WebApplication.CreateBuilder(args);

// Carregar appsettings.local.json se existir para permitir overrides locais
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

// 1. Configurar as opções (Settings) do appsettings.json

builder.Services.Configure<OpenAiSettings>(builder.Configuration.GetSection("OpenAi"));
builder.Services.Configure<PersonalInfoSettings>(builder.Configuration.GetSection("PersonalInfo"));
builder.Services.Configure<TemplateSettings>(builder.Configuration.GetSection("Template"));
builder.Services.Configure<CandidateDataSettings>(builder.Configuration.GetSection("CandidateData"));

// Configurar o DbContext do SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=CvAutomation.db"));

// 2. Adicionar suporte a Controllers
builder.Services.AddControllers();

// 3. Adicionar CORS para permitir acesso do frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 4. Injeção de Dependência dos Serviços de Aplicação
builder.Services.AddSingleton<IResumeBlockCache, ResumeBlockCache>();
builder.Services.AddSingleton<IKeywordCache, KeywordCache>();
builder.Services.AddScoped<IKeywordExtractionService, KeywordExtractionService>();
builder.Services.AddScoped<IContentGenerationService, ContentGenerationService>();
builder.Services.AddScoped<ILatexTemplateService, LatexTemplateService>();
builder.Services.AddScoped<IResumeOrchestrationService, ResumeOrchestrationService>();
builder.Services.AddScoped<IPdfGenerationService, PdfGenerationService>();
builder.Services.AddScoped<IResumeDatabaseService, ResumeDatabaseService>();

// 5. Configurar HttpClient tipado para o OpenAiService com resiliência via Polly (exponential backoff: 2s, 4s, 8s)
builder.Services.AddHttpClient<IAiService, OpenAiService>()
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests) // Trata rate limit se houver
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}
await DbInitializer.SeedAsync(app.Services);

// Configurar o pipeline do request
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();

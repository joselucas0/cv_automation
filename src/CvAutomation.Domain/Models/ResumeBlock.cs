using System;

namespace CvAutomation.Domain.Models;

public class ResumeBlock
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string Type { get; set; } = string.Empty; // "summary", "experience", "skill", "project", "certification"
    
    public string Title { get; set; } = string.Empty; // Ex: "Full Stack Developer", "QA Engineer"
    
    public string Company { get; set; } = string.Empty; // Empresa (opcional)
    
    public string Location { get; set; } = "Campo Grande, MS";
    
    public string Period { get; set; } = string.Empty; // Ex: "Set 2025 -- Atual"
    
    public string Content { get; set; } = string.Empty; // O texto real LaTeX-ready que vai para o currículo
    
    public string SemanticContent { get; set; } = string.Empty; // Metadados ricos usados no embedding
    
    public string TechTagsJson { get; set; } = "[]"; // Serializado como JSON array e.g., ["C#", ".NET"]
    
    public string AtsKeywordsJson { get; set; } = "[]"; // Serializado como JSON array
    
    public string Seniority { get; set; } = "pleno"; // "junior", "pleno", "senior"
    
    public int Priority { get; set; } = 0; // Prioridade/Peso manual
    
    public bool Active { get; set; } = true;
    
    public string EmbeddingJson { get; set; } = "[]"; // Array de 1536 floats serializado em JSON

    // Novos campos para suporte à automação avançada de RAG
    public string StackContext { get; set; } = string.Empty; // Ex: "C# / .NET", "Java / Spring", "React"
    
    public string CompanyKey { get; set; } = string.Empty; // Ex: "eleve", "digix", "sigeamb", "decubitocare"
    
    public bool IsWildcard { get; set; } = false; // Se pode adotar qualquer stack (Eleve)
    
    public string JuniorSpecialties { get; set; } = string.Empty; // Descrição de especialidades para Júnior
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

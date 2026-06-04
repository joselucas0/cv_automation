using System;

namespace CvAutomation.Domain.Models;

public class GeneratedResume
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string Title { get; set; } = string.Empty; // Ex: "cvJoseGoogle"
    
    public string CompanyName { get; set; } = string.Empty; // Nome da empresa
    
    public string JobKeywordsJson { get; set; } = "[]"; // Palavras-chave extraídas da vaga
    
    public string JobDescription { get; set; } = string.Empty; // Requisitos/Descrição da vaga original
    
    public string GeneratedTexPath { get; set; } = string.Empty;
    
    public string GeneratedPdfPath { get; set; } = string.Empty;
    
    public string UsedBlocksJson { get; set; } = "[]"; // Lista de UUIDs dos blocos utilizados
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

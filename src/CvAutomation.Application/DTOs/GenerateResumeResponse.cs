using System.Collections.Generic;

namespace CvAutomation.Application.DTOs;

public record GenerateResumeResponse(
    string LatexContent, 
    List<string> ExtractedKeywords, 
    string? PdfBase64 = null,
    double CoverageScore = 1.0,
    List<string>? Warnings = null,
    string? FileName = null);

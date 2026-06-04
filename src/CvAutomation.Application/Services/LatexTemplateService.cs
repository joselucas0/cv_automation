using System;
using System.Collections.Generic;
using System.IO;
using CvAutomation.Application.Interfaces;
using CvAutomation.Application.Options;
using CvAutomation.Domain.Models;
using Microsoft.Extensions.Options;

namespace CvAutomation.Application.Services;

public class LatexTemplateService : ILatexTemplateService
{
    private readonly PersonalInfoSettings _personalInfo;
    private readonly TemplateSettings _templateSettings;

    public LatexTemplateService(
        IOptions<PersonalInfoSettings> personalInfo,
        IOptions<TemplateSettings> templateSettings)
    {
        _personalInfo = personalInfo.Value;
        _templateSettings = templateSettings.Value;
    }

    public string GenerateLatex(
        TitleAndAboutContent titleAbout,
        SkillsContent skills,
        string experiencesSectionLatex)
    {
        if (string.IsNullOrWhiteSpace(_templateSettings.TemplatePath))
            throw new InvalidOperationException("O caminho do template LaTeX não foi configurado.");

        if (!File.Exists(_templateSettings.TemplatePath))
            throw new FileNotFoundException($"O template LaTeX base não foi encontrado no caminho: {_templateSettings.TemplatePath}");

        var templateContent = File.ReadAllText(_templateSettings.TemplatePath);

        // Substituições dinâmicas gerais
        var result = templateContent
            .Replace("{{PDF_TITLE}}", titleAbout.PdfTitle)
            .Replace("{{ABOUT_ME}}", titleAbout.AboutMe)
            .Replace("{{SKILLS_ITEMS}}", skills.SkillsLatex)
            .Replace("{{EXPERIENCES_SECTION}}", experiencesSectionLatex);

        // Substituições de informações pessoais fixas
        result = result
            .Replace("{{FULL_NAME}}", _personalInfo.FullName)
            .Replace("{{LOCATION}}", _personalInfo.Location)
            .Replace("{{EMAIL}}", _personalInfo.Email)
            .Replace("{{LINKEDIN}}", _personalInfo.LinkedIn)
            .Replace("{{GITHUB}}", _personalInfo.GitHub);

        return result;
    }
}

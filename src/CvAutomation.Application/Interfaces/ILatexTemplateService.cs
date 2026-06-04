using System.Collections.Generic;
using CvAutomation.Domain.Models;

namespace CvAutomation.Application.Interfaces;

public interface ILatexTemplateService
{
    string GenerateLatex(TitleAndAboutContent titleAbout, SkillsContent skills, string experiencesSectionLatex);
}

using System.Collections.Generic;

namespace CvAutomation.Domain.Models;

public class AtsKeywords
{
    public string JobTitle { get; set; } = string.Empty;
    public List<string> HardSkills { get; set; } = [];
    public List<string> SoftSkills { get; set; } = [];
    public List<string> Tools { get; set; } = [];
    public List<string> KeyResponsibilities { get; set; } = [];
    public string Seniority { get; set; } = string.Empty;
}

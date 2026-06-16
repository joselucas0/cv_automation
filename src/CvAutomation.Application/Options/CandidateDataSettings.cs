using System.Collections.Generic;

namespace CvAutomation.Application.Options;

public class CandidateDataSettings
{
    public Dictionary<string, BaseExperienceInfo> Experiences { get; set; } = [];
}

public class BaseExperienceInfo
{
    public string CompanyName { get; set; } = string.Empty;
    public string BaseActuation { get; set; } = string.Empty;
    public string BaseItems { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public bool IsWildcard { get; set; }
    public string JuniorSpecialties { get; set; } = string.Empty;
    public int Priority { get; set; } = 1;
}


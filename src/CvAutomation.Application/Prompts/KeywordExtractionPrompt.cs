namespace CvAutomation.Application.Prompts;

public static class KeywordExtractionPrompt
{
    public const string Template = @"Você é um especialista em ATS (Applicant Tracking System). Analise a descrição da vaga abaixo e extraia as keywords mais relevantes para otimização de currículo.

Retorne APENAS um JSON válido no seguinte formato:
{
  ""jobTitle"": ""título exato da vaga"",
  ""hardSkills"": [""skill1"", ""skill2""],
  ""softSkills"": [""soft1"", ""soft2""],
  ""tools"": [""tool1"", ""tool2""],
  ""keyResponsibilities"": [""resp1"", ""resp2""],
  ""seniority"": ""Junior|Pleno|Senior""
}

Descrição da vaga:
{jobDescription}";
}

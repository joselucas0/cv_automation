namespace CvAutomation.Application.DTOs;

public record GenerateResumeRequest(string JobDescription, string CompanyName, string JobTitle = "");

# --- Build Stage ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy CSPROJ files and restore dependencies
COPY CvAutomation.slnx ./
COPY src/CvAutomation.Domain/CvAutomation.Domain.csproj src/CvAutomation.Domain/
COPY src/CvAutomation.Application/CvAutomation.Application.csproj src/CvAutomation.Application/
COPY src/CvAutomation.Infrastructure/CvAutomation.Infrastructure.csproj src/CvAutomation.Infrastructure/
COPY src/CvAutomation.Api/CvAutomation.Api.csproj src/CvAutomation.Api/

RUN dotnet restore src/CvAutomation.Api/CvAutomation.Api.csproj

# Copy all source files and publish
COPY . .
RUN dotnet publish src/CvAutomation.Api/CvAutomation.Api.csproj -c Release -o out

# --- Runtime Stage ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install pdflatex (TeX Live) for PDF generation
RUN apt-get update && apt-get install -y --no-install-recommends \
    texlive-latex-base \
    texlive-latex-recommended \
    texlive-fonts-recommended \
    texlive-latex-extra \
    && rm -rf /var/lib/apt/lists/*

# Copy build artifacts
COPY --from=build /app/out .

EXPOSE 5005
ENV ASPNETCORE_URLS=http://+:5005

ENTRYPOINT ["dotnet", "CvAutomation.Api.dll"]

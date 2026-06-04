using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CvAutomation.Application.Interfaces;

namespace CvAutomation.Infrastructure.Services;

public class PdfGenerationService : IPdfGenerationService
{
    public async Task<byte[]> GeneratePdfAsync(string latexContent, CancellationToken ct = default)
    {
        // 1. Cria um diretório temporário no workspace para compilação isolada
        var baseTempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pdf_temp");
        if (!Directory.Exists(baseTempPath))
        {
            Directory.CreateDirectory(baseTempPath);
        }

        var uniqueId = Guid.NewGuid().ToString("N");
        var workDir = Path.Combine(baseTempPath, uniqueId);
        Directory.CreateDirectory(workDir);

        var texFilePath = Path.Combine(workDir, "resume.tex");
        var pdfFilePath = Path.Combine(workDir, "resume.pdf");

        // 2. Grava o conteúdo LaTeX no arquivo .tex
        await File.WriteAllTextAsync(texFilePath, latexContent, System.Text.Encoding.UTF8, ct);

        // 3. Configura a execução do pdflatex (do MiKTeX)
        var startInfo = new ProcessStartInfo
        {
            FileName = "pdflatex",
            // -interaction=nonstopmode evita travamentos por erro de sintaxe do LaTeX
            // --miktex-disable-installer impede janelas popups pedindo pra instalar pacotes
            Arguments = $"-interaction=nonstopmode --miktex-disable-installer -output-directory=\"{workDir}\" \"{texFilePath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = true,
            WorkingDirectory = workDir
        };

        using var process = new Process();
        process.StartInfo = startInfo;

        try
        {
            // 4. Inicia o processo
            process.Start();

            // 5. Aguarda o término de forma assíncrona com suporte a CancellationToken
            await process.WaitForExitAsync(ct);

            // 6. Verifica se o PDF foi gerado com sucesso
            if (!File.Exists(pdfFilePath))
            {
                var logFilePath = Path.Combine(workDir, "resume.log");
                var logContent = "Nenhum arquivo de log encontrado.";
                if (File.Exists(logFilePath))
                {
                    logContent = await File.ReadAllTextAsync(logFilePath, ct);
                }
                throw new InvalidOperationException($"Falha na compilação do LaTeX. O PDF não foi gerado.\nLog do MiKTeX:\n{logContent}");
            }

            // 7. Lê os bytes do arquivo PDF gerado
            var pdfBytes = await File.ReadAllBytesAsync(pdfFilePath, ct);
            return pdfBytes;
        }
        finally
        {
            // 8. Limpa os arquivos temporários de forma isolada e segura
            try
            {
                if (Directory.Exists(workDir))
                {
                    Directory.Delete(workDir, recursive: true);
                }
            }
            catch
            {
                // Suprime qualquer erro secundário de I/O na deleção
            }
        }
    }
}

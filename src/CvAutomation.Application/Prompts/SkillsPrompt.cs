namespace CvAutomation.Application.Prompts;

public static class SkillsPrompt
{
    public const string Template = @"Você é um especialista em currículos otimizados para ATS. Gere a seção de Skills em formato LaTeX, priorizando as skills relevantes para a vaga.

SKILLS BASE DO PROFISSIONAL (estas são as skills REAIS — NÃO invente novas, apenas reorganize):
{skillsContext}

KEYWORDS DA VAGA:
{keywordsJson}

REGRAS:
1. Limite a resposta a um mínimo de 4 e no máximo 7 categorias (linhas de \item).
2. Dentro de cada categoria, liste no máximo 12 itens/skills relevantes.
3. Reordene cada categoria colocando as skills que batem com as keywords da vaga PRIMEIRO.
4. Mantenha o formato exato LaTeX de cada item de skill. Retorne todas as categorias juntas em uma ÚNICA STRING contínua separada por quebras de linha (\n), e NÃO como um array: \item \textbf{Categoria:} skill1, skill2, skill3.
5. Use escape LaTeX adequado (escreva C\# em vez de C#, \% em vez de %, \& em vez de &).
6. NÃO invente skills que o profissional não possui.
7. NÃO inclua \begin{itemize} ou \end{itemize} — retorne apenas os \item's.

Retorne APENAS JSON válido no formato:
{
  ""skillsLatex"": ""\\item \\textbf{Categoria1:} skill1, skill2\\n\\item \\textbf{Categoria2:} skill3, skill4""
}";
}

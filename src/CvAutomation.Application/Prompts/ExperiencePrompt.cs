namespace CvAutomation.Application.Prompts;

public static class ExperiencePrompt
{
    public const string Template = @"Você é um especialista em currículos otimizados para ATS. Reescreva a descrição da experiência profissional do candidato de forma que ela enfatize e incorpore de forma natural as keywords da vaga.

DADOS DA EMPRESA:
- Empresa/Projeto: {experienceName}

DIRETRIZES E REGRAS DESTA EXPERIÊNCIA:
{companyContext}

DADOS DE EXPERIÊNCIA BASE DO CANDIDATO (NÃO altere dados factuais, métricas ou cargos — apenas reorganize e ajuste a redação para enfatizar a vaga seguindo as Diretrizes e Regras acima):
- Parágrafo de atuação base: {baseActuation}
- Entregas base (itens):
{baseItems}

KEYWORDS DA VAGA:
{keywordsJson}

REGRAS GERAIS:
1. Reescreva o parágrafo de atuação base de forma a incorporar as keywords relevantes da vaga NATURALMENTE. Ele deve ser um texto curto (2 a 3 linhas).
2. Reordene as entregas (itens) colocando as mais relevantes para a vaga PRIMEIRO.
3. Ajuste a redação de cada item para destacar keywords pedidas, sem alterar o sentido factual ou inventar dados/métricas.
4. Siga RIGOROSAMENTE as Diretrizes e Regras Desta Experiência acima (ex: se for Júnior e restrita a certas stacks, não fale de outras).
5. Em qualquer empresa, é perfeitamente aceitável e incentivado mencionar Git/GitHub/Git Flow e Banco de Dados (PostgreSQL, SQL Server, SQL) se fizer sentido.
6. Use escape LaTeX corretamente (escreva C\# em vez de C#, \% em vez de %, & em vez de \& ou similar - no LaTeX o & comercial deve ser escrito como \&).
7. Mantenha o formato com \item para cada entrega.
8. NÃO inclua \begin{itemize} ou \end{itemize} — retorne apenas os \item's.

Retorne APENAS JSON válido no formato:
{
  ""experienceSummary"": ""Atuação no desenvolvimento..."",
  ""experienceItems"": ""\\item Desenvolvimento backend...\\n\\item Outra entrega...""
}";
}

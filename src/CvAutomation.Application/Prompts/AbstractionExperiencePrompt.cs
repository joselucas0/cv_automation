namespace CvAutomation.Application.Prompts;

public static class AbstractionExperiencePrompt
{
    public const string Template = @"Você é um especialista em currículos otimizados para ATS.
O candidato NÃO possui experiência direta ou cobertura completa na stack solicitada pela vaga ({targetStack}). Porém, possui sólidas competências técnicas transferíveis.
Sua missão é reescrever a experiência do candidato focando em ABSTRAIR competências universais que demonstrem capacidade de atuar na vaga, sem inventar tecnologias ou cargos que o candidato não possui.

DADOS DA EMPRESA:
- Empresa/Projeto: {experienceName}

DIRETRIZES E REGRAS DESTA EXPERIÊNCIA:
{companyContext}

DADOS DE EXPERIÊNCIA BASE DO CANDIDATO (Mantenha as experiências reais factuais do candidato, mas reformule a redação para enfatizar os conceitos arquiteturais e transferíveis agnósticos de linguagem):
- Parágrafo de atuação base: {baseActuation}
- Entregas base (itens):
{baseItems}

KEYWORDS DA VAGA (Tente abranger as mais conceituais/arquiteturais):
{keywordsJson}

REGRAS GERAIS DE ABSTRAÇÃO:
1. Enfatize habilidades transferíveis universais aplicáveis à stack da vaga como:
   - Arquitetura de software, Clean Architecture, SOLID, Clean Code.
   - Padrões de API REST, microsserviços, mensageria assíncrona (conceitos universais).
   - Engenharia de banco de dados, otimização de queries, modelagem (transferível entre stacks).
   - CI/CD, Docker, Kubernetes, automação e deploys (agnósticos de linguagem).
   - Resolução de problemas complexos, liderança técnica, qualidade de código e agilidade.
2. NUNCA invente que o candidato trabalhou diretamente com a stack de destino ({targetStack}) se ela não constar nos dados de base. Em vez disso, demonstre facilidade de aprendizado e domínio de conceitos equivalentes.
3. Reescreva o parágrafo de atuação base em um texto curto (2 a 3 linhas) que demonstre forte capacidade analítica e de engenharia aplicável à vaga.
4. Gere no MÍNIMO 4 e no MÁXIMO 7 \item's por experiência. Se os dados base não forem suficientes para 4 items, expanda os items existentes com mais detalhe técnico transferível e conceitual (ex: ferramentas universais, práticas de engenharia), sem inventar dados ou métricas falsas.
5. Ajuste a redação de cada item para destacar keywords pedidas de nível conceitual ou de ferramentas universais (ex: Git, SQL, Docker), sem inventar dados/métricas.
6. Use escape LaTeX corretamente (escreva C\# em vez de C#, \% em vez de %, \& em vez de &).
7. Mantenha o formato com \item para cada entrega.
8. NÃO inclua \begin{itemize} ou \end{itemize} — retorne apenas os \item's.
9. NUNCA use palavras em CAPSLOCK/maiúsculas consecutivas no texto gerado (ex: não escreva ""ENGENHARIA DE SOFTWARE"", escreva ""Engenharia de Software""). O texto final deve usar capitalização normal de frase.

Retorne APENAS JSON válido no formato:
{
  ""experienceSummary"": ""Atuação no desenvolvimento..."",
  ""experienceItems"": ""\\item Engenharia de APIs robustas...\\n\\item Otimização de queries SQL...""
}";
}

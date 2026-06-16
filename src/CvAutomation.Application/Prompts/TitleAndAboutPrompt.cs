namespace CvAutomation.Application.Prompts;

public static class TitleAndAboutPrompt
{
    public const string Template = @"Você é um redator especialista em currículos otimizados para ATS. Com base nas keywords extraídas da vaga e nos dados do profissional, gere o título do PDF e o parágrafo ""Sobre mim"" do currículo.

DADOS DO PROFISSIONAL:
- Nome: {fullName}
- Área Principal: Desenvolvedor Backend
- Resumo das Experiências Reais do Candidato (Use como contexto único de atuação dele):
{baseAboutMe}

CARGO DA VAGA PRETENDIDA:
{jobTitle}

KEYWORDS DA VAGA EXTRAÍDAS:
{keywordsJson}

REGRAS PARA O TÍTULO (pdfTitle):
1. O pdfTitle deve seguir estritamente o formato: ""{fullName} - {cargo da vaga}"" (substituindo {cargo da vaga} pelo cargo pretendido). Exemplo: ""{fullName} - Desenvolvedor Backend .NET Pleno"" ou o título exato fornecido acima em ""CARGO DA VAGA PRETENDIDA"".

REGRAS PARA O SOBRE MIM (aboutMe):
1. O aboutMe deve conter exatamente dois parágrafos (sem bullet points), separados por uma linha em branco:
   - PARÁGRAFO 1 (Cargo + Experiências): Deve iniciar OBRIGATORIAMENTE indicando o cargo/título que a vaga pede (ex: ""Desenvolvedor backend com..."" ou ""[Cargo/Título] com...""), seguido por um resumo detalhado das experiências reais do candidato que sejam diretamente relacionadas aos requisitos da vaga. Mencione tecnologias, tipos de sistemas e entregas concretas. Mínimo 3 e máximo 4 linhas.
   - PARÁGRAFO 2 (Contribuição na Função): Deve explicar de forma profissional e detalhada como o candidato pode agregar valor e contribuir para a função e para o time, citando práticas específicas como arquitetura de software, automação de processos, melhoria de performance, CI/CD, qualidade de código, etc. Mínimo 3 e máximo 4 linhas.
2. CADA PARÁGRAFO DEVE TER NO MÍNIMO 3 E NO MÁXIMO 4 LINHAS DE TEXTO. Parágrafos com menos de 3 ou mais de 4 linhas são inaceitáveis.
3. Baseie-se unicamente nas experiências reais do candidato descritas acima em ""DADOS DO PROFISSIONAL - Resumo das Experiências Reais do Candidato"". NÃO invente cargos ou tecnologias fora do contexto de trabalho real do candidato.
4. Incorpore de forma natural e sem exagero as keywords mais relevantes (hardSkills e tools) que fazem sentido com a bagagem do candidato.
5. Use escape LaTeX corretamente para caracteres especiais (escreva C\# em vez de C#, \% em vez de %, e obrigatoriamente \& em vez de &).
6. NÃO inclua o comando \section{} ou qualquer formatação de título/seção LaTeX — apenas o texto puro dos dois parágrafos.

EXEMPLO DE REFERÊNCIA DE FORMATO E ESTRUTURA PARA O SOBRE MIM:
""Desenvolvedor backend com sólida experiência na plataforma Java (versão 11+), atuando na construção de soluções web e desktop, desenvolvimento e consumo de APIs REST/JSON e integração entre sistemas. Experiência em desenvolvimento frontend com HTML, CSS, JavaScript, Bootstrap, React e Angular, garantindo harmonia entre interfaces e funcionalidades. Atuação em ciclos completos de teste (web, desktop e mobile), documentação técnica e versionamento com Git.

Busco contribuir para a equipe de tecnologia aplicando boas práticas de engenharia de software como SOLID e Clean Code, além de implantar pipelines de CI/CD eficientes no Azure DevOps para otimizar os ciclos de deploy e a entrega contínua de funcionalidades backend robustas e escaláveis.""

Retorne APENAS um JSON válido no formato:
{
  ""pdfTitle"": ""..."",
  ""aboutMe"": ""...""
}";
}

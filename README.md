# 🚀 ATS Resume Optimizer

O **ATS Resume Optimizer** é um sistema inteligente de otimização de currículos baseado em **RAG (Retrieval-Augmented Generation)**, desenvolvido com **.NET 10 (C#)** no backend, **Ollama** local para busca semântica, **Groq** para geração de conteúdo em altíssima velocidade e um frontend moderno responsivo.

O objetivo do projeto é analisar a descrição de uma vaga de emprego (requisitos/keywords), identificar quais partes das experiências, habilidades e resumos reais do candidato são mais aderentes à vaga e gerar um currículo PDF customizado e diagramado em **LaTeX** otimizado para sistemas ATS (Applicant Tracking Systems).

![ATS Resume Optimizer Front](screenshot.png)

---

## 🛠️ Tecnologias & Arquitetura

O projeto foi construído seguindo as boas práticas do ecossistema .NET e princípios de **Clean Architecture**:

* **CvAutomation.Domain**: Modelos de dados de domínio (blocos de currículo, logs de geração).
* **CvAutomation.Application**: Regras de negócio, interfaces de IA, DTOs e prompts do sistema.
* **CvAutomation.Infrastructure**: Serviços de infraestrutura, incluindo integração HTTP com OpenAI/Groq, RAG semântico, acesso à base SQLite com Entity Framework Core e compilação do LaTeX para PDF.
* **CvAutomation.Api**: Controllers REST que servem o frontend.

### Componentes Técnicos Relevantes:
* **Fila Concorrente com Bounded Channel (`System.Threading.Channels`)**: O backend inicializa um canal concorrente assíncrono produtor-consumidor para processar as 6 tarefas de escrita de blocos (sobre mim, skills e as 4 experiências) em paralelo com 3 workers concorrentes, maximizando o throughput de chamadas às APIs de LLM.
* **Embeddings Locais (Ollama)**: Utiliza o modelo local `nomic-embed-text` (768 dimensões) para criar embeddings da vaga e fazer a busca semântica vetorial (similaridade de cosseno) no banco de dados SQLite. **Custo zero e sem limites de cota.**
* **Groq API**: Integração com a API do Groq usando o modelo flagship `llama-3.3-70b-versatile` para responder em JSON estruturado a geração de cada bloco em frações de segundos.
* **PDF Compilation via LaTeX**: Gera arquivos `.tex` estruturados de forma dinâmica a partir de um template base e compila o PDF físico no diretório de saídas.

---

## 🧠 Funcionamento do Pipeline (Passo a Passo)

1. **Entrada**: O usuário insere o nome da empresa, título do cargo e descrição completa da vaga no frontend.
2. **Extração de Keywords**: O backend faz uma chamada de extração para extrair hard skills, soft skills e tools necessárias para a vaga.
3. **Busca Semântica (RAG)**:
   - A descrição da vaga e o cargo são convertidos em vetores usando o **Ollama** (`nomic-embed-text`).
   - O sistema busca no SQLite o resumo profissional e os pools de experiências mais semelhantes à vaga com base na similaridade cosseno.
   - Caso a aderência de stack seja inferior a **40%**, o sistema entra no **Modo de Abstração**, reescrevendo as experiências do candidato com foco em competências transferíveis e engenharia pura para evitar invenções ou mentiras.
4. **Fila de Geração Paralela**: O `Channel` processa em concorrência as tarefas de geração usando o **Groq** em JSON Mode.
5. **Diagramação LaTeX**: Os blocos gerados são interpolados no [base_template.tex](templates/base_template.tex).
6. **Compilação e Histórico**: O PDF é compilado e salvo na pasta `exports/` e o histórico de geração com as keywords utilizadas é salvo no banco de dados.

---

## 🚀 Como Executar o Projeto Localmente

### 1. Pré-requisitos
* **.NET 10 SDK** instalado.
* **Python 3.x** instalado.
* **Ollama** rodando na máquina com o modelo de embeddings:
  ```bash
  ollama pull nomic-embed-text
  ```
* Uma chave de API do **Groq** (criada gratuitamente no painel da Groq).

### 2. Configurando o Ambiente
Crie um arquivo `.env` na raiz do projeto contendo as suas chaves e endpoints:
```text
# OpenAI Key (caso queira usar embeddings OpenAI, mas o padrão é usar Ollama local)
OPENAI_API_KEY=sua_chave_openai_aqui

# Configuração do Groq para geração de textos acelerada
GROQ_API_KEY=sua_chave_groq_aqui
GROQ_BASE_URL=https://api.groq.com/openai/v1
GROQ_MODEL=llama-3.3-70b-versatile

# Configuração de Embeddings Locais Gratuitas via Ollama
EMBEDDING_BASE_URL=http://localhost:11434/v1
EMBEDDING_MODEL=nomic-embed-text
```

### 3. Semeando o Banco de Dados (SQLite)
Com o Ollama rodando localmente, execute o script python para processar e gerar os embeddings dos seus blocos reais de currículo no banco SQLite:
```bash
python scripts/seed_database.py
```

### 4. Executando o Backend API
Inicie o servidor local .NET (porta `5005`):
```powershell
dotnet run --project src/CvAutomation.Api --launch-profile http
```

### 5. Executando o Frontend
Você pode abrir diretamente no seu navegador o arquivo `client/index.html` ou servi-lo localmente via Node:
```bash
npx serve client
```

// ═══════════════════════════════════════════
// TYPES & DTOs
// ═══════════════════════════════════════════
interface GenerateResumeResponse {
    latexContent: string;
    extractedKeywords: string[];
    pdfBase64?: string;
    coverageScore?: number;
    warnings?: string[];
    fileName?: string;
}

// ═══════════════════════════════════════════
// API CONFIGURATION
// ═══════════════════════════════════════════
const API_URL = 'http://localhost:5005/api/resume/generate';

// ═══════════════════════════════════════════
// DOM ELEMENTS
// ═══════════════════════════════════════════
const txtCompanyName = document.getElementById('company-name') as HTMLInputElement;
const txtJobTitle = document.getElementById('job-title') as HTMLInputElement;
const txtJobDescription = document.getElementById('job-description') as HTMLTextAreaElement;
const btnGenerate = document.getElementById('btn-generate') as HTMLButtonElement;
const btnCopy = document.getElementById('btn-copy') as HTMLButtonElement;
const btnDownload = document.getElementById('btn-download') as HTMLButtonElement;

const placeholderState = document.getElementById('output-placeholder') as HTMLDivElement;
const loadingState = document.getElementById('output-loading') as HTMLDivElement;
const resultState = document.getElementById('output-result') as HTMLDivElement;

const loadingStepText = document.getElementById('loading-step') as HTMLParagraphElement;
const keywordsList = document.getElementById('keywords-list') as HTMLDivElement;
const latexOutput = document.getElementById('latex-output') as HTMLPreElement;

// DOM Elements for Coverage Panel
const coveragePanel = document.getElementById('coverage-panel') as HTMLDivElement;
const coverageBadge = document.getElementById('coverage-badge') as HTMLSpanElement;
const coverageBarFill = document.getElementById('coverage-bar-fill') as HTMLDivElement;
const coverageDescription = document.getElementById('coverage-description') as HTMLParagraphElement;
const coverageWarnings = document.getElementById('coverage-warnings') as HTMLDivElement;

// ═══════════════════════════════════════════
// STATE MANAGERS
// ═══════════════════════════════════════════
let stepIntervalId: number | undefined;
let currentPdfBase64: string | null = null;
let currentFileName: string | null = null;

function startLoadingStepSimulation() {
    const steps = [
        { time: 0, text: '🔍 Enviando requisição para o backend .NET...' },
        { time: 1500, text: '🤖 OpenAI extraindo as keywords mais relevantes da vaga (ATS)...' },
        { time: 4000, text: '🧠 Gerando embedding da vaga via text-embedding-3-small...' },
        { time: 6500, text: '⚡ Rodando busca semântica RAG e similaridade vetorial no SQLite...' },
        { time: 9000, text: '📥 Inicializando fila concorrente Bounded Channel no backend...' },
        { time: 11000, text: '🔄 Otimizando dinamicamente apenas os 3 blocos mais aderentes...' },
        { time: 14000, text: '📝 Montando template LaTeX & gerando arquivos físicos em /exports...' },
        { time: 17500, text: '🚀 Pronto! Salvando histórico GeneratedResume na base SQLite...' }
    ];

    let currentStep = 0;
    loadingStepText.textContent = steps[0].text;

    // Cancela qualquer simulação anterior ativa
    if (stepIntervalId) {
        clearInterval(stepIntervalId);
    }

    const startTime = Date.now();

    stepIntervalId = setInterval(() => {
        const elapsed = Date.now() - startTime;
        
        // Encontra o passo correspondente ao tempo decorrido
        for (let i = steps.length - 1; i >= 0; i--) {
            if (elapsed >= steps[i].time) {
                if (currentStep !== i) {
                    currentStep = i;
                    loadingStepText.textContent = steps[i].text;
                }
                break;
            }
        }
    }, 500);
}

function stopLoadingStepSimulation() {
    if (stepIntervalId) {
        clearInterval(stepIntervalId);
        stepIntervalId = undefined;
    }
}

// ═══════════════════════════════════════════
// EVENT LISTENERS
// ═══════════════════════════════════════════

btnGenerate.addEventListener('click', async () => {
    const company = txtCompanyName.value.trim();
    const jobTitle = txtJobTitle.value.trim();
    const description = txtJobDescription.value.trim();

    if (!company) {
        alert('Por favor, insira o nome da empresa.');
        txtCompanyName.focus();
        return;
    }

    if (!jobTitle) {
        alert('Por favor, insira o título da vaga.');
        txtJobTitle.focus();
        return;
    }

    if (!description) {
        alert('Por favor, insira a descrição da vaga antes de otimizar.');
        txtJobDescription.focus();
        return;
    }

    // 1. Alterna para estado de loading
    placeholderState.classList.add('hidden');
    resultState.classList.add('hidden');
    loadingState.classList.remove('hidden');
    
    // Inicia spinner do botão
    const spinner = btnGenerate.querySelector('.spinner') as HTMLElement;
    const btnText = btnGenerate.querySelector('.btn-text') as HTMLElement;
    spinner.classList.remove('hidden');
    btnText.textContent = 'Otimizando...';
    btnGenerate.disabled = true;

    startLoadingStepSimulation();

    try {
        // 2. Envia request HTTP POST para o Backend .NET
        const response = await fetch(API_URL, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ 
                jobDescription: description,
                companyName: company,
                jobTitle: jobTitle
            })
        });

        if (!response.ok) {
            const errData = await response.json();
            throw new Error(errData?.error || `Erro do Servidor: ${response.status}`);
        }

        const data: GenerateResumeResponse = await response.json();

        // 3. Renderiza os resultados com sucesso
        renderResults(data);

    } catch (error: any) {
        console.error(error);
        alert(`Erro ao otimizar currículo: ${error.message}`);
        
        // Retorna para o estado inicial em caso de erro
        loadingState.classList.add('hidden');
        placeholderState.classList.remove('hidden');
    } finally {
        // 4. Restaura estado do botão e para simulação de passos
        stopLoadingStepSimulation();
        spinner.classList.add('hidden');
        btnText.textContent = 'Otimizar Currículo';
        btnGenerate.disabled = false;
        loadingState.classList.add('hidden');
    }
});

btnCopy.addEventListener('click', () => {
    const latexText = latexOutput.textContent || '';
    if (!latexText) return;

    navigator.clipboard.writeText(latexText)
        .then(() => {
            const originalText = btnCopy.textContent;
            btnCopy.textContent = 'Copiado! ✓';
            btnCopy.classList.add('btn-primary');
            btnCopy.classList.remove('btn-secondary');
            
            setTimeout(() => {
                btnCopy.textContent = originalText;
                btnCopy.classList.remove('btn-primary');
                btnCopy.classList.add('btn-secondary');
            }, 2000);
        })
        .catch(err => {
            console.error('Erro ao copiar:', err);
            alert('Não foi possível copiar para a área de transferência.');
        });
});

btnDownload.addEventListener('click', () => {
    if (!currentPdfBase64) return;

    try {
        // Converte base64 para ArrayBuffer
        const byteCharacters = atob(currentPdfBase64);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);
        const blob = new Blob([byteArray], { type: 'application/pdf' });
        
        // Cria link e engatilha o download
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = currentFileName ? `${currentFileName}.pdf` : 'curriculo_otimizado.pdf';
        document.body.appendChild(link);
        link.click();
        
        // Limpa referências
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    } catch (err) {
        console.error('Erro ao baixar o PDF:', err);
        alert('Não foi possível gerar e baixar o arquivo PDF.');
    }
});

// ═══════════════════════════════════════════
// RENDERING FUNCTIONS
// ═══════════════════════════════════════════

function renderResults(data: GenerateResumeResponse) {
    // 1. Renderiza o Painel de Cobertura
    if (data.coverageScore !== undefined) {
        coveragePanel.classList.remove('hidden');
        
        const score = data.coverageScore;
        const scorePct = Math.round(score * 100);
        coverageBarFill.style.width = `${scorePct}%`;
        
        // Limpa classes anteriores
        coverageBadge.className = 'coverage-badge';
        coverageBarFill.className = 'coverage-bar-fill';
        
        if (score >= 0.7) {
            coverageBadge.classList.add('high');
            coverageBadge.innerHTML = `🟢 Alta Aderência (${scorePct}%)`;
            coverageBarFill.classList.add('high');
            coverageDescription.textContent = 'Currículo altamente otimizado e alinhado com a vaga. Os blocos de experiência cobrem perfeitamente os requisitos!';
        } else if (score >= 0.4) {
            coverageBadge.classList.add('medium');
            coverageBadge.innerHTML = `🟡 Média Aderência (${scorePct}%)`;
            coverageBarFill.classList.add('medium');
            coverageDescription.textContent = 'Currículo adaptado de forma aceitável. Algumas competências foram transferidas para cobrir a vaga. Revise os detalhes.';
        } else {
            coverageBadge.classList.add('low');
            coverageBadge.innerHTML = `🔴 Baixa Aderência (${scorePct}%)`;
            coverageBarFill.classList.add('low');
            coverageDescription.textContent = 'Stack não coberta na base de experiências. Geramos o currículo com abstração de habilidades e competências transferíveis.';
        }
        
        // Renderiza Warnings
        if (data.warnings && data.warnings.length > 0) {
            coverageWarnings.classList.remove('hidden');
            coverageWarnings.innerHTML = data.warnings.map(warning => 
                `<div class="warning-item">⚠️ ${warning}</div>`
            ).join('');
        } else {
            coverageWarnings.classList.add('hidden');
            coverageWarnings.innerHTML = '';
        }
    } else {
        coveragePanel.classList.add('hidden');
    }

    // 2. Renderiza as keywords (Chips)
    keywordsList.innerHTML = '';
    if (data.extractedKeywords && data.extractedKeywords.length > 0) {
        data.extractedKeywords.forEach(keyword => {
            const chip = document.createElement('span');
            chip.className = 'chip';
            chip.textContent = keyword;
            keywordsList.appendChild(chip);
        });
    } else {
        const noKeywords = document.createElement('span');
        noKeywords.style.color = 'var(--text-muted)';
        noKeywords.style.fontSize = '0.9rem';
        noKeywords.textContent = 'Nenhuma keyword extraída.';
        keywordsList.appendChild(noKeywords);
    }

    // 3. Renderiza o código LaTeX formatado
    latexOutput.textContent = data.latexContent;

    // 4. Verifica e gerencia o botão de PDF
    if (data.pdfBase64) {
        currentPdfBase64 = data.pdfBase64;
        currentFileName = data.fileName || null;
        btnDownload.classList.remove('hidden');
    } else {
        currentPdfBase64 = null;
        currentFileName = null;
        btnDownload.classList.add('hidden');
    }

    // 5. Exibe o painel de resultados
    loadingState.classList.add('hidden');
    placeholderState.classList.add('hidden');
    resultState.classList.remove('hidden');
}

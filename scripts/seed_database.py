import os
import re
import sys
import uuid
import sqlite3
import json
from datetime import datetime

# Read .env file to get configuration
env_path = os.path.join(os.path.dirname(__file__), "..", ".env")
env_vars = {}
if os.path.exists(env_path):
    with open(env_path, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line or line.startswith("#"):
                continue
            parts = line.split("=", 1)
            if len(parts) == 2:
                env_vars[parts[0].strip()] = parts[1].strip()

# Embeddings settings
emb_base_url = env_vars.get("EMBEDDING_BASE_URL") or os.environ.get("EMBEDDING_BASE_URL") or "https://api.openai.com/v1"
emb_model = env_vars.get("EMBEDDING_MODEL") or os.environ.get("EMBEDDING_MODEL") or "text-embedding-3-small"
emb_api_key = env_vars.get("EMBEDDING_API_KEY") or os.environ.get("EMBEDDING_API_KEY") or env_vars.get("OPENAI_API_KEY") or os.environ.get("OPENAI_API_KEY")

is_local = "localhost" in emb_base_url or "127.0.0.1" in emb_base_url or "::1" in emb_base_url
if not emb_api_key and not is_local:
    print("Error: EMBEDDING_API_KEY or OPENAI_API_KEY not found in .env file or environment!")
    sys.exit(1)

from openai import OpenAI
emb_client = OpenAI(base_url=emb_base_url, api_key=emb_api_key or "dummy_key")

def get_embedding(text):
    text = text.replace("\n", " ")
    response = emb_client.embeddings.create(
        input=[text],
        model=emb_model
    )
    return response.data[0].embedding

def parse_sobre_mim(file_path):
    with open(file_path, "r", encoding="utf-8") as f:
        content = f.read()
    
    # Split by markdown dividers '---'
    blocks = content.split("---")
    summaries = []
    
    for idx, block in enumerate(blocks):
        block = block.strip()
        if not block:
            continue
        
        # We find lines starting with '# ' as the header/title
        lines = block.split("\n")
        title = ""
        paragraph_lines = []
        
        for line in lines:
            line_str = line.strip()
            if line_str.startswith("# "):
                title = line_str.replace("# ", "").strip()
            elif line_str:
                paragraph_lines.append(line_str)
                
        paragraph = " ".join(paragraph_lines)
        if title and paragraph:
            summaries.append({
                "title": title,
                "content": paragraph
            })
            
    return summaries

def parse_skills(file_path):
    with open(file_path, "r", encoding="utf-8") as f:
        content = f.read()
        
    categories = []
    parts = content.split("---")
    for part in parts:
        part = part.strip()
        if not part:
            continue
        
        lines = part.split("\n")
        category_name = None
        skills = []
        
        for line in lines:
            line_str = line.strip()
            if line_str.startswith("## "):
                category_name = line_str.replace("## ", "").strip()
            elif line_str.startswith("* "):
                skill = line_str.replace("* ", "").strip()
                if skill:
                    skills.append(skill)
                    
        if category_name and skills:
            categories.append({
                "category": category_name,
                "skills": skills
            })
            
    return categories

def parse_experience_pools(file_path):
    with open(file_path, "r", encoding="utf-8") as f:
        content = f.read()
        
    pools = []
    parts = content.split("---")
    for part in parts:
        part = part.strip()
        if not part:
            continue
            
        lines = part.split("\n")
        header = None
        items = []
        
        for line in lines:
            line_str = line.strip()
            if line_str.startswith("# "):
                if "Base de Experiências" in line_str:
                    continue
                header = line_str.replace("# ", "").strip()
            elif line_str.startswith("* "):
                item = line_str.replace("* ", "").strip()
                if item:
                    items.append(item)
                    
        if header and items:
            parts_header = re.split(r'[—\-]', header)
            stack = parts_header[0].strip()
            
            seniority = "pleno"
            if len(parts_header) > 1:
                seniority_str = parts_header[-1].strip().lower()
                if "júnior" in seniority_str or "junior" in seniority_str or "jr" in seniority_str:
                    seniority = "junior"
                    
            pools.append({
                "title": header,
                "stack": stack,
                "seniority": seniority,
                "items": items
            })
            
    return pools

def main():
    db_dir = os.path.join(os.path.dirname(__file__), "..", "src", "CvAutomation.Api")
    os.makedirs(db_dir, exist_ok=True)
    db_path = os.path.join(db_dir, "CvAutomation.db")
    
    print(f"Connecting to SQLite database at {db_path}...")
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    
    # Create tables
    cursor.execute("""
    CREATE TABLE IF NOT EXISTS ResumeBlocks (
        Id TEXT PRIMARY KEY,
        Type TEXT NOT NULL,
        Title TEXT,
        Company TEXT,
        Location TEXT,
        Period TEXT,
        Content TEXT,
        SemanticContent TEXT,
        TechTagsJson TEXT,
        AtsKeywordsJson TEXT,
        Seniority TEXT,
        Priority INTEGER NOT NULL,
        Active INTEGER NOT NULL,
        EmbeddingJson TEXT,
        StackContext TEXT,
        CompanyKey TEXT,
        IsWildcard INTEGER NOT NULL,
        JuniorSpecialties TEXT,
        CreatedAt TEXT NOT NULL,
        UpdatedAt TEXT NOT NULL
    )
    """)
    
    cursor.execute("""
    CREATE TABLE IF NOT EXISTS GeneratedResumes (
        Id TEXT PRIMARY KEY,
        Title TEXT NOT NULL,
        CompanyName TEXT NOT NULL,
        JobDescription TEXT,
        JobKeywordsJson TEXT,
        GeneratedTexPath TEXT,
        GeneratedPdfPath TEXT,
        UsedBlocksJson TEXT,
        CreatedAt TEXT NOT NULL
    )
    """)
    
    # Clear existing ResumeBlocks
    print("Clearing existing ResumeBlocks...")
    cursor.execute("DELETE FROM ResumeBlocks")
    
    base_dir = os.path.join(os.path.dirname(__file__), "..", "base")
    
    # 1. Seed summaries from sobreMim.md
    print("Seeding summaries from sobreMim.md...")
    sobre_mim_path = os.path.join(base_dir, "sobreMim.md")
    summaries = parse_sobre_mim(sobre_mim_path)
    for s in summaries:
        title = s["title"]
        content = s["content"]
        
        clean_title = re.sub(r'^\d+\.\s*', '', title).strip()
        
        seniority = "pleno"
        if "Júnior" in clean_title or "junior" in clean_title.lower() or "jr" in clean_title.lower():
            seniority = "junior"
        elif "Sênior" in clean_title or "senior" in clean_title.lower():
            seniority = "senior"
            
        semantic_content = f"Tipo: resumo / sobre mim. Título: {clean_title}. Conteúdo: {content}"
        print(f"  -> Generating embedding for summary: {clean_title}...")
        emb = get_embedding(semantic_content)
        
        block_id = str(uuid.uuid4())
        now = datetime.utcnow().isoformat()
        
        cursor.execute("""
        INSERT INTO ResumeBlocks (
            Id, Type, Title, Company, Location, Period, Content, SemanticContent,
            TechTagsJson, AtsKeywordsJson, Seniority, Priority, Active, EmbeddingJson,
            StackContext, CompanyKey, IsWildcard, JuniorSpecialties, CreatedAt, UpdatedAt
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        """, (
            block_id, "summary", clean_title, "", "", "", content, semantic_content,
            "[]", "[]", seniority, 10, 1, json.dumps(emb),
            "", "", 0, "", now, now
        ))
        
    # 2. Seed skills from skills.md
    print("Seeding skills from skills.md...")
    skills_path = os.path.join(base_dir, "skills.md")
    skills_categories = parse_skills(skills_path)
    for sc in skills_categories:
        cat = sc["category"]
        skills_list = sc["skills"]
        skills_str = ", ".join(skills_list)
        
        semantic_content = f"Habilidades de {cat}: {skills_str}"
        print(f"  -> Generating embedding for skill category: {cat}...")
        emb = get_embedding(semantic_content)
        
        block_id = str(uuid.uuid4())
        now = datetime.utcnow().isoformat()
        
        cursor.execute("""
        INSERT INTO ResumeBlocks (
            Id, Type, Title, Company, Location, Period, Content, SemanticContent,
            TechTagsJson, AtsKeywordsJson, Seniority, Priority, Active, EmbeddingJson,
            StackContext, CompanyKey, IsWildcard, JuniorSpecialties, CreatedAt, UpdatedAt
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        """, (
            block_id, "skill", cat, "", "", "", json.dumps(skills_list), semantic_content,
            json.dumps(skills_list), "[]", "pleno", 5, 1, json.dumps(emb),
            cat, "", 0, "", now, now
        ))
        
    # 3. Seed exp.md pools
    print("Seeding experience pools from exp.md...")
    exp_path = os.path.join(base_dir, "exp.md")
    exp_pools = parse_experience_pools(exp_path)
    for p in exp_pools:
        title = p["title"]
        stack = p["stack"]
        seniority = p["seniority"]
        items = p["items"]
        items_latex = "\n".join([f"\\item {item}" for item in items])
        
        semantic_content = f"Pool de experiências para {stack} ({seniority}). Itens: " + " ".join(items)
        print(f"  -> Generating embedding for exp pool: {title}...")
        emb = get_embedding(semantic_content)
        
        block_id = str(uuid.uuid4())
        now = datetime.utcnow().isoformat()
        
        cursor.execute("""
        INSERT INTO ResumeBlocks (
            Id, Type, Title, Company, Location, Period, Content, SemanticContent,
            TechTagsJson, AtsKeywordsJson, Seniority, Priority, Active, EmbeddingJson,
            StackContext, CompanyKey, IsWildcard, JuniorSpecialties, CreatedAt, UpdatedAt
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        """, (
            block_id, "experience_pool", title, "", "", "", items_latex, semantic_content,
            "[]", "[]", seniority, 5, 1, json.dumps(emb),
            stack, "", 0, "", now, now
        ))
        
    # 4. Seed 4 company experiences
    print("Seeding 4 company experiences...")
    appsettings_path = os.path.join(os.path.dirname(__file__), "..", "src", "CvAutomation.Api", "appsettings.json")
    with open(appsettings_path, "r", encoding="utf-8") as f:
        appsettings = json.load(f)
        
    experiences_config = appsettings["CandidateData"]["Experiences"]
    
    companies_metadata = {
        "eleve": {
            "Company": "Eleve Software",
            "Period": "Set 2025 -- Atual",
            "Location": "Campo Grande, MS",
            "IsWildcard": 1,
            "JuniorSpecialties": "Qualquer stack solicitada na vaga (coringa)",
            "Priority": 5
        },
        "digix": {
            "Company": "Digix",
            "Period": "Ago 2024 -- Set 2025",
            "Location": "Campo Grande, MS",
            "IsWildcard": 0,
            "JuniorSpecialties": "C# / .NET, IA / Inteligência Artificial, análise de dados, qualidade de código, entregas, automações, Git, banco de dados",
            "Priority": 4
        },
        "sigeamb": {
            "Company": "Laboratório de Análise da Qualidade da Água - UFMS",
            "Period": "Jul 2025 -- Dez 2025",
            "Location": "Campo Grande, MS",
            "IsWildcard": 0,
            "JuniorSpecialties": "Arquitetura backend, modelagem de banco de dados PostgreSQL, deploys, Docker, Nginx como proxy reverso, Git, banco de dados",
            "Priority": 3
        },
        "decubitocare": {
            "Company": "Hospital Universitário - HU-UFMS",
            "Period": "Jan 2025 -- Jul 2025",
            "Location": "Campo Grande, MS",
            "IsWildcard": 0,
            "JuniorSpecialties": "Garantia de qualidade de software, automação de testes (Cypress, testes de API, funcionais, regressão), arquitetura, implantação, desenvolvimento, CI/CD, Git, banco de dados",
            "Priority": 2
        }
    }
    
    for key, meta in companies_metadata.items():
        config = experiences_config[key]
        company_name = config["CompanyName"]
        base_actuation = config["BaseActuation"]
        base_items = config["BaseItems"]
        
        semantic_content = f"Empresa: {company_name}. Atuação Geral: {base_actuation}. Conquistas: {base_items}"
        print(f"  -> Generating embedding for company experience: {company_name}...")
        emb = get_embedding(semantic_content)
        
        block_id = str(uuid.uuid4())
        now = datetime.utcnow().isoformat()
        
        cursor.execute("""
        INSERT INTO ResumeBlocks (
            Id, Type, Title, Company, Location, Period, Content, SemanticContent,
            TechTagsJson, AtsKeywordsJson, Seniority, Priority, Active, EmbeddingJson,
            StackContext, CompanyKey, IsWildcard, JuniorSpecialties, CreatedAt, UpdatedAt
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        """, (
            block_id, "experience", "Desenvolvedor de Software", company_name, meta["Location"], meta["Period"],
            base_items, base_actuation, "[]", "[]", "pleno", meta["Priority"], 1, json.dumps(emb),
            "", key, meta["IsWildcard"], meta["JuniorSpecialties"], now, now
        ))
        
    conn.commit()
    conn.close()
    print("Successfully seeded all data in CvAutomation.db!")

if __name__ == "__main__":
    main()

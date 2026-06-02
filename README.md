# Docs2Pdf

Aplicação web para conversão de arquivos Office e imagens em PDF.

- `frontend/`: frontend Angular 17
- `backend/`: backend .NET 8 Web API
- `backend/Services/GotenbergConversionService.cs`: Integração com Gotenberg para conversão rápida e confiável

## 🚀 Como começar

### Opção 1: Docker Compose (Recomendado)

```bash
# Inicie toda a stack (API + Gotenberg + Frontend)
docker-compose up -d

# Verifique saúde
curl http://localhost:3000/health        # Gotenberg
curl http://localhost/api/convert/health # API

# Frontend
http://localhost:4200
```

### Opção 2: Desenvolvimento Local

#### Frontend

```bash
cd frontend
npm install
npm run start
# Acesse: http://localhost:4200
```

#### Backend

```bash
cd backend
dotnet run
# Acesse: http://localhost:5000
```

> **Nota:** Requer Gotenberg rodando em `http://localhost:3000`

## 🧪 Testes Locais

Antes de commitar, execute os testes:

### Windows (PowerShell)
```powershell
./quick-test.ps1
```

### Linux / macOS (Bash)
```bash
chmod +x quick-test.sh
./quick-test.sh
```

**Tempo esperado:** ~5 minutos

Para testes manuais, consulte [TESTING_EXAMPLES.md](TESTING_EXAMPLES.md)

## 🌐 Deployment em Produção (Render)

Para fazer deploy em produção no Render:

1. **Consulte [RENDER_DEPLOYMENT.md](RENDER_DEPLOYMENT.md)** para instruções completas
2. Resumo rápido:
   - API e Gotenberg rodam como serviços separados
   - Use `render.yaml` para Infrastructure as Code
   - Configure variáveis de ambiente: `GotenbergUrl=http://gotenberg:3000`

```bash
# Depois de testar localmente
git push origin main
# O Render fará deploy automaticamente
```

> **Importante:** Gotenberg **não** pode ser deployado no mesmo processo da API.

## 📋 Documentação

- **[QUICK_START.md](QUICK_START.md)** - Guia rápido de testes
- **[LOCAL_TESTING_GUIDE.md](LOCAL_TESTING_GUIDE.md)** - Guia completo de testes
- **[TESTING_EXAMPLES.md](TESTING_EXAMPLES.md)** - Exemplos prontos para copiar/colar
- **[GOTENBERG_MIGRATION.md](GOTENBERG_MIGRATION.md)** - Informações sobre integração com Gotenberg
- **[TECHNICAL_DETAILS.md](TECHNICAL_DETAILS.md)** - Arquitetura e detalhes técnicos

## 🎯 Fluxo de Desenvolvimento

1. **Testes Locais** → Execute `quick-test.ps1` (Windows) ou `quick-test.sh` (Linux/Mac)
2. **Verificação de Logs** → `docker-compose logs -f`
3. **Commit** → `git commit` (após testes passarem)
4. **Push** → `git push`

## ⚡ Performance

| Método | Tempo/Arquivo | Inicialização |
|--------|---------------|----|
| LibreOffice (antigo) | 8-10s | Requer startup |
| Gotenberg (novo) | 1-2s | Persistente ✨ |

**Ganho:** 5-10x mais rápido 🚀

## 🛑 Parar Tudo

```bash
# Parar containers (mantém dados)
docker-compose down

# Parar e limpar tudo
docker-compose down -v

# Remover imagens (libera espaço)
docker-compose down --rmi all
```

## ⚙️ Variáveis de Ambiente

- `GotenbergUrl` - URL do Gotenberg (padrão: `http://localhost:3000`)
- `FrontendOrigin` - CORS origin (padrão: `http://localhost:4200`)
- `ASPNETCORE_URLS` - URLs da API (padrão: `http://+:80`)

Veja `.env.example` para mais detalhes.

## 📝 Observações

- Sem testes unitários automatizados (validação manual)
- Docker Compose orquestra toda a stack
- Gotenberg é executado como serviço persistente
- LibreOffice foi removido do ambiente Docker

# Implementação Gotenberg - Detalhes Técnicos

## 📋 Resumo das Mudanças

### Arquivos Criados

| Arquivo | Propósito |
|---------|-----------|
| `GotenbergConversionService.cs` | Novo serviço que comunica com Gotenberg via HTTP |
| `Dockerfile.gotenberg` | Dockerfile otimizado sem LibreOffice |
| `docker-compose.yml` | Orquestração de Gotenberg + API |
| `appsettings.Gotenberg.json` | Configuração para ambiente Gotenberg |
| `GOTENBERG_MIGRATION.md` | Guia de migração e uso |
| `.env.example` | Exemplo de variáveis de ambiente |
| `check-health.sh` | Script para verificar saúde dos serviços |
| `test-conversion.sh` | Script para testar conversões |

### Arquivos Modificados

| Arquivo | Mudança |
|---------|---------|
| `PdfConversionService.cs` | Refatorado para delegar a Gotenberg |
| `Program.cs` | Adicionado registro de `GotenbergConversionService` |
| `Dockerfile` | Mantido original, novo: `Dockerfile.gotenberg` |

---

## 🏗️ Arquitetura

### Antes (LibreOffice)
```
HTTP Request
    ↓
PdfConversionService
    ↓
Process.Start("soffice")
    ↓
LibreOffice inicia (~5s)
    ↓
Conversão (~3-5s)
    ↓
Process finaliza
    ↓
Return PDF
```
**Tempo total: 8-10 segundos por arquivo**

### Depois (Gotenberg)
```
HTTP Request
    ↓
PdfConversionService
    ↓
GotenbergConversionService
    ↓
HTTP POST para Gotenberg (já rodando)
    ↓
Conversão (~1-2s)
    ↓
Return PDF
```
**Tempo total: 1-2 segundos por arquivo**

---

## 🔧 Implementação Técnica

### GotenbergConversionService

#### Responsabilidades
1. **Comunicação HTTP**: Envia arquivos para Gotenberg
2. **Retry Logic**: Trata ServiceUnavailable com retry automático
3. **Endpoint Selection**: Escolhe endpoint correto baseado no tipo de arquivo
4. **Logging**: Registra conversões e erros
5. **Health Check**: Verifica disponibilidade do Gotenberg

#### Endpoints Suportados
- `/forms/libreoffice/convert` - Office documents
- `/forms/chromium/convert/html` - HTML documents

#### Retry Strategy
- 3 tentativas máximas
- Delay exponencial: 500ms, 1000ms, 1500ms
- Apenas retenta em ServiceUnavailable ou timeout

```csharp
private const int MaxRetries = 3;
private const int RetryDelayMs = 500;
```

### PdfConversionService

#### Antes
```csharp
public async Task<byte[]> ConvertAsync(IFormFile file)
{
    // Encontrava LibreOffice
    // Salvava arquivo temporário
    // Iniciava processo com Process.Start()
    // Esperava conclusão
    // Retornava resultado
}
```

#### Depois
```csharp
public async Task<byte[]> ConvertAsync(IFormFile file)
{
    return await _gotenbergService.ConvertAsync(file);
}
```

**Simplificado e delegado para Gotenberg.**

### Injeção de Dependência

**Program.cs:**
```csharp
builder.Services.AddHttpClient<GotenbergConversionService>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));

builder.Services.AddSingleton<PdfConversionService>();
```

**HttpClientFactory** reutiliza conexões para melhor performance.

---

## 🐳 Docker

### Dockerfile.gotenberg

Mudanças principais:
```dockerfile
# ❌ Removido: LibreOffice (250MB)
# RUN apt-get install libreoffice libreoffice-core ...

# ✅ Adicionado: curl apenas (para health checks)
RUN apt-get install -y --no-install-recommends curl
```

**Tamanho da imagem:** ~120MB (vs ~400MB com LibreOffice)

### docker-compose.yml

Orquestração:
```yaml
services:
  gotenberg:
    image: gotenberg/gotenberg:8.7
    ports:
      - "3000:3000"
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:3000/health"]
  
  api:
    depends_on:
      gotenberg:
        condition: service_healthy
```

**Gotenberg inicia primeiro e prova saúde antes da API iniciar.**

---

## 📊 Performance Esperada

### Antes (LibreOffice)
```
Single File:     8-10 segundos
10 Files:        80-100 segundos
Combined:        80-100 segundos
```

### Depois (Gotenberg)
```
Single File:     1-2 segundos (5-10x mais rápido)
10 Files:        10-20 segundos (5-10x mais rápido)
Combined:        10-20 segundos (5-10x mais rápido)
```

### Fatores que afetam performance
- CPU (Gotenberg é CPU-intensive)
- Memória disponível
- Tamanho do arquivo
- Complexidade do documento

---

## 🔐 Configuração

### Variáveis de Ambiente

```bash
# URL do Gotenberg (pode ser local ou remoto)
GotenbergUrl=http://gotenberg:3000

# CORS origin para frontend
FrontendOrigin=http://localhost:4200

# Gotenberg LibreOffice settings
GOTENBERG_LIBREOFFICE_DISABLE_GPU=true
GOTENBERG_LIBREOFFICE_DISABLE_UI_DEFAULTS=true
```

### appsettings.json

```json
{
  "GotenbergUrl": "http://gotenberg:3000",
  "FrontendOrigin": "http://localhost:4200",
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

---

## 🧪 Testes

### Teste Manual
```bash
# Start services
docker-compose up -d

# Test conversion
curl -X POST \
  -F "files=@document.docx" \
  http://localhost/api/convert \
  -o output.pdf

# Check health
curl http://localhost:3000/health
curl http://localhost/api/convert/health
```

### Verificar Logs
```bash
# API logs
docker-compose logs -f api

# Gotenberg logs
docker-compose logs -f gotenberg
```

---

## ⚠️ Possíveis Problemas

### 1. Gotenberg não conecta
```
Error: Unable to connect to http://localhost:3000
```
**Solução:**
```bash
docker-compose logs gotenberg
curl http://localhost:3000/health
```

### 2. Timeout na conversão
```
Error: Request timeout after 30s
```
**Causas:**
- Gotenberg sobrecarregado
- Arquivo muito grande
- Insuficiente CPU/Memória

**Solução:**
```bash
# Aumentar limite de tamanho de arquivo em Program.cs
# ou
# Aumentar memória do Gotenberg em docker-compose.yml
# ou
# Verificar recursos do sistema
docker stats
```

### 3. Conversão retorna 500
```
HTTP 500 - Internal Server Error
```
**Solução:**
```bash
# Ver erro detalhado nos logs
docker-compose logs gotenberg | tail -50

# Gotenberg pode não suportar o formato
# Testar com arquivo diferente
```

---

## 🔄 Migração Gradual (Opcional)

Se precisar manter LibreOffice como fallback:

```csharp
public async Task<byte[]> ConvertAsync(IFormFile file)
{
    try
    {
        return await _gotenbergService.ConvertAsync(file);
    }
    catch (Exception ex)
    {
        _logger.LogWarning($"Gotenberg failed, using LibreOffice: {ex.Message}");
        return await _libreOfficeService.ConvertAsync(file);
    }
}
```

---

## 📈 Monitoramento

### Métricas Importantes

1. **Taxa de sucesso de conversão**
2. **Tempo médio de conversão**
3. **Tamanho máximo de arquivo**
4. **Uso de CPU/Memória**

### Ferramentas Recomendadas
- Prometheus + Grafana
- Application Insights
- New Relic

---

## 🚀 Deployment

### Local Development
```bash
docker-compose up -d
# API em http://localhost:80
# Gotenberg em http://localhost:3000
```

### Production
```bash
# Use docker-compose com override
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d

# Ou configure manualmente
# - Gotenberg em um serviço separado com scale-up
# - API com múltiplas instâncias
# - Load balancer na frente
```

---

## 📚 Recursos Adicionais

- [Gotenberg Documentation](https://gotenberg.dev/)
- [Gotenberg API Reference](https://gotenberg.dev/docs/routes)
- [Docker Compose Reference](https://docs.docker.com/compose/compose-file/)
- [ASP.NET HttpClientFactory](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests)

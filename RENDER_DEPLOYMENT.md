# Configuração de Produção no Render

## Arquitetura

A aplicação Docs2Pdf em produção usa dois serviços:

1. **docs2pdf-api**: ASP.NET Core API (.NET 8)
   - Recebe uploads de arquivos
   - Valida e encaminha para conversão
   - Retorna PDF gerado

2. **gotenberg**: Serviço de conversão de documentos
   - Converte Office, LibreOffice, HTML → PDF
   - Roda como container Docker
   - Acessível via HTTP interno dentro do Render

## Deployment no Render

### Opção 1: Usando Infrastructure as Code (Recomendado)

1. **Faça push do código** (já pronto):
   ```bash
   git push origin main
   ```

2. **No Render Dashboard**:
   - Vá para seu projeto
   - Clique em **Infrastructure as Code**
   - Cole o conteúdo de `render.yaml`
   - Clique em **Create**

   Isso criará automaticamente:
   - Serviço da API (Pull Request Preview + Production)
   - Serviço Gotenberg (Production)

### Opção 2: Criando Serviços Manualmente

#### Serviço 1: Gotenberg

1. **New > Web Service**
2. **Settings**:
   - Name: `gotenberg`
   - Image URL: `gotenberg/gotenberg:8.7`
   - Plan: **Free** (ou Starter, dependendo da demanda)
   - Exposed Port: `3000` (padrão do Gotenberg)
   - Environment Variables:
     ```
     LOG_LEVEL=info
     ```
   - Health Check Path: `/health`

3. **Deploy**

#### Serviço 2: Docs2Pdf API

1. **New > Web Service**
2. **Connect to GitHub**:
   - Select repository: `Docs2Pdf`
   - Branch: `main`
3. **Settings**:
   - Name: `docs2pdf-api`
   - Build Command: `cd backend && dotnet publish -c Release -o out`
   - Start Command: `./out/Docs2Pdf.Api`
   - Plan: **Free** (ou conforme necessário)
   - Exposed Port: `80`
4. **Environment Variables** (crítico):
   ```
   ASPNETCORE_ENVIRONMENT=Production
   ASPNETCORE_URLS=http://+:80
   GotenbergUrl=http://gotenberg:3000
   FrontendOrigin=https://docs2pdf-api.onrender.com
   ```

5. **Deploy**

## Pontos Importantes

### 1. Comunicação entre Serviços (CRÍTICO para Plano Free)

⚠️ **No plano Free do Render, serviços separados NÃO conseguem se comunicar por nome de host interno!**

- ❌ `http://gotenberg:3000` → Falha: "Name or service not known"
- ✅ `https://gotenberg.onrender.com` → Funciona: URL pública

**Por quê?**
- Hostname interno (`gotenberg:3000`) só funciona com **Private Network** (planos pagos)
- Plano Free: cada serviço é isolado, só pode acessar por URL pública

**Solução implementada:**
```csharp
// No GotenbergConversionService
_gotenbergUrl = configuration["GotenbergUrl"] 
    ?? (env == "Production" ? "https://gotenberg.onrender.com" : "http://localhost:3000");
```

### 2. URLs Públicas vs Privadas

| Ambiente | Gotenberg URL | Notas |
|----------|---|---|
| **Local** | `http://localhost:3000` | Docker Compose |
| **Render Free** | `https://gotenberg.onrender.com` | ✅ URL pública obrigatória |
| **Render Starter+** | `http://gotenberg:3000` | Opcional: usar Private Network |

### 3. Nomes dos Serviços Render

Quando criar no Render, use **exatamente** estes nomes:
- `docs2pdf-api` → públicamente acessível em `https://docs2pdf-api.onrender.com`
- `gotenberg` → públicamente acessível em `https://gotenberg.onrender.com`

### 4. Variáveis de Ambiente

**No `render.yaml` ou manualmente:**
```
GotenbergUrl=https://gotenberg.onrender.com
FrontendOrigin=https://docs2pdf-api.onrender.com
```

⚠️ **Importante:** Use `https://` para produção (obrigatório no Render).

- Spin-down após 15 min de inatividade
- Limite de memória: ~512MB por serviço
- Se precisar mais performance, use plan Starter

## Verificação pós-deployment

### Testar a API

```bash
curl -X POST https://docs2pdf-api.onrender.com/api/convert/health
# Deve retornar 200 OK
```

### Testar Conversão

```bash
curl -X POST https://docs2pdf-api.onrender.com/api/convert \
  -F "files=@documento.txt"
# Deve retornar PDF
```

## Troubleshooting

### "Name or service not known (gotenberg:3000)"

- ✗ **Causa:** Tentando usar hostname interno em plano Free
- ✓ **Solução:** 
  1. Verificar nome do serviço Gotenberg no Render (deve ser `gotenberg`)
  2. Verificar variável `GotenbergUrl` está como `https://gotenberg.onrender.com`
  3. Aguardar ~30s para o serviço ficar disponível (spin-down)
  4. Tentar novamente

### "Connection refused (localhost:3000)" (Produção)

- ✗ **Causa:** Localhost não existe em ambiente Render
- ✓ **Solução:** Verificar `ASPNETCORE_ENVIRONMENT=Production` está definido
  - Se Development, carrega `appsettings.json` (localhost)
  - Se Production, carrega `appsettings.Production.json` (HTTPS públicas)

### "502 Bad Gateway"

- Possível: Serviço em spin-down ou inicializando
- Aguarde 30-60 segundos e tente novamente
- Verifique logs no Render Dashboard

### "Conversion timeout"

- Timeout padrão aumentado para 30s (era 5s)
- Se ainda ocorrer, considere plano Starter

### "HTTPS certificate error"

- Render usa certificado válido automaticamente
- Se ver erro de certificado, é possivelmente firewall local
- Em produção: deve funcionar normalmente

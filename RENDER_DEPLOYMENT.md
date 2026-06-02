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

### 1. Comunicação entre Serviços

Os serviços no Render podem se comunicar usando:
- Nome do serviço + `:` + porta interna
- Ex: `http://gotenberg:3000` (dentro de `docs2pdf-api`)

Não use `localhost` em produção!

### 2. URLs Públicas

Cada serviço recebe uma URL pública:
- API: `https://docs2pdf-api.onrender.com`
- Gotenberg: `https://gotenberg.onrender.com` (opcional, só interno)

### 3. Configuração de Ambiente

A variável `GotenbergUrl` deve ser:
- **Desenvolvimento local**: `http://localhost:3000`
- **Produção Render**: `http://gotenberg:3000`

Isso já está configurado no arquivo `render.yaml`.

### 4. Limites do Plano Free

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

### "Connection refused (localhost:3000)"

- ✗ API está tentando usar `localhost`
- ✓ Solução: Verificar se `GotenbergUrl` está configurado como variável de ambiente

### "502 Bad Gateway"

- Possível: Gotenberg está em spin-down
- Aguarde alguns segundos e tente novamente

### "Conversion timeout"

- Aumentar o plan ou otimizar conversão
- Considere usar plano Starter

## Rollback

Se algo der errado:

1. Remova os serviços do Render
2. Volte para a última versão funcional
3. Redeploye com ajustes

```bash
git log --oneline  # Ver commits
git revert <commit-hash>
git push origin main
```

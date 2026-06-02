# Migração para Gotenberg

## Por que Gotenberg?

A migração de LibreOffice para Gotenberg oferece **benefícios significativos**:

### 🚀 Performance
- **LibreOffice**: Inicializa do zero para cada conversão (~5-10 segundos por arquivo)
- **Gotenberg**: Serviço persistente, conversão em ~1-2 segundos por arquivo
- **Melhoria**: 5-10x mais rápido ⚡

### 💪 Confiabilidade
- Gotenberg é otimizado para conversões em lote
- Melhor tratamento de erros e timeouts
- Suporta retry automático
- Isolamento de processos (containers)

### 📦 Simplicidade
- Sem dependências do SO (LibreOffice não precisa estar instalado)
- Docker-ready
- Configuração via variáveis de ambiente
- Healthcheck integrado

### 📋 Compatibilidade
Suporta os mesmos formatos:
- **Office**: Word (.docx, .doc), Excel (.xlsx, .xls), PowerPoint (.pptx, .ppt)
- **Outros**: PDF, HTML, ODT, ODS, ODP, RTF, imagens

---

## Como Usar

### Opção 1: Docker Compose (Recomendado)

```bash
# Inicie toda a stack (API + Gotenberg)
docker-compose up -d

# Verificar status
docker-compose logs -f

# Parar
docker-compose down
```

### Opção 2: Gotenberg local

```bash
# Instale Gotenberg (exemplo: macOS com Homebrew)
brew install gotenberg

# Ou com Docker
docker run -p 3000:3000 gotenberg/gotenberg:8.7

# Configure a URL no appsettings.json
```

### Opção 3: Gotenberg remoto

Configure em `appsettings.json`:
```json
{
  "GotenbergUrl": "http://seu-server-gotenberg:3000"
}
```

---

## Arquitetura

```
Frontend (Angular)
    ↓
API .NET (PdfConversionService)
    ↓
GotenbergConversionService
    ↓
Gotenberg Container (http://localhost:3000)
    ↓
Conversão de documentos
```

---

## Configuração

### Environment Variables

- `GotenbergUrl`: URL do Gotenberg (padrão: `http://localhost:3000`)
- `FrontendOrigin`: CORS origin (padrão: `http://localhost:4200`)
- `ASPNETCORE_URLS`: URLs da API (padrão: `http://+:80`)

### appsettings.json

```json
{
  "GotenbergUrl": "http://gotenberg:3000",
  "FrontendOrigin": "http://localhost:4200"
}
```

---

## Estrutura de Arquivos Novos

```
backend/
├── Services/
│   ├── GotenbergConversionService.cs   (Novo)
│   ├── PdfConversionService.cs          (Refatorado - usa Gotenberg)
│   └── ...
├── Dockerfile.gotenberg                 (Novo - sem LibreOffice)
├── appsettings.Gotenberg.json          (Novo)
└── ...

/
├── docker-compose.yml                   (Novo)
```

---

## Endpoints Gotenberg Utilizados

| Formato | Endpoint |
|---------|----------|
| Office (Word, Excel, PowerPoint) | `/forms/libreoffice/convert` |
| HTML | `/forms/chromium/convert/html` |
| Imagens | Processadas localmente |

---

## Troubleshooting

### Gotenberg não conecta
```bash
# Verificar saúde
curl http://localhost:3000/health

# Verificar logs
docker logs gotenberg
```

### Conversão lenta
- Verifique memória disponível
- Aumente workers do Gotenberg: `GOTENBERG_LIBREOFFICE_UNOCONV_LISTENER_START_PORT`

### Arquivo muito grande
- Limite padrão: 500MB (configurável em `Program.cs`)
- Gotenberg timeout padrão: 30s

---

## Migração Gradual

Se ainda precisar do LibreOffice como fallback, você pode:

1. Manter `PdfConversionService` com fallback para LibreOffice
2. Adicionar feature flag para escolher backend
3. Monitorar conversões bem-sucedidas antes de remover LibreOffice

---

## Próximos Passos Recomendados

✅ Teste a conversão com vários tipos de arquivo  
✅ Monitore performance e memória  
✅ Configure backup/failover do Gotenberg  
✅ Atualize documentação da API  
✅ Remova LibreOffice do ambiente de produção  

---

## Recursos

- [Gotenberg Docs](https://gotenberg.dev/)
- [PdfSharpCore Docs](https://github.com/ststeiger/PdfSharpCore)
- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/)

# 🧪 Guia de Testes Locais

## ✅ Pré-requisitos

Antes de começar, verifique:

```bash
# Docker instalado?
docker --version
# Docker Compose instalado?
docker-compose --version
# Git (para commitar depois)
git --version
```

---

## 📋 Checklist de Testes

### Fase 1: Inicialização

- [ ] Docker Compose inicia sem erros
- [ ] Gotenberg fica healthy
- [ ] API inicia e responde

### Fase 2: Conversões Básicas

- [ ] Arquivo TXT → PDF
- [ ] Arquivo DOCX → PDF
- [ ] Arquivo XLSX → PDF
- [ ] Arquivo JPG → PDF

### Fase 3: Casos de Uso Complexos

- [ ] Múltiplos arquivos (combine=false)
- [ ] Múltiplos arquivos com combine=true
- [ ] Arquivo muito grande (>100MB)
- [ ] Formato não suportado (erro apropriado)

### Fase 4: Verificações de Performance

- [ ] Tempo de conversão < 5 segundos
- [ ] Memória não cresce infinitamente
- [ ] Gotenberg não para de responder

---

## 🚀 Passo a Passo

### 1️⃣ Limpar Ambiente Anterior (Importante!)

```bash
# Parar containers existentes
docker-compose down -v

# Remover imagens antigas (opcional)
docker-compose down --rmi all

# Limpar espaço (opcional)
docker system prune -f
```

### 2️⃣ Iniciar Services

```bash
# No diretório raiz do projeto
docker-compose up -d

# Verificar logs
docker-compose logs -f

# Ou em outro terminal, verificar status
docker-compose ps
```

**Esperado:**
```
STATUS         PORTS
healthy        0.0.0.0:3000->3000/tcp    (Gotenberg)
healthy        0.0.0.0:80->80/tcp        (API)
```

### 3️⃣ Verificar Saúde

```bash
# Health check Gotenberg
curl http://localhost:3000/health

# Health check API
curl http://localhost:80/api/convert/health

# Resposta esperada
{"status":"ok"}
```

### 4️⃣ Testar Conversões

#### Teste 1: Arquivo TXT simples

```bash
cd /tmp  # ou pasta temporária

# Criar arquivo de teste
echo "Este é um teste de conversão para PDF" > test.txt
echo "Data: $(date)" >> test.txt

# Converter
curl -X POST \
  -F "files=@test.txt" \
  http://localhost/api/convert \
  -o test.pdf

# Verificar resultado
ls -lh test.pdf
file test.pdf  # Deve dizer "PDF document"
```

#### Teste 2: Arquivo Word (DOCX)

```bash
# Criar DOCX de teste (pode usar um existente ou libreoffice)
# Ou baixar sample
wget https://sample-videos.com/doc/Sample1.docx -O sample.docx 2>/dev/null || \
curl -L https://sample-videos.com/doc/Sample1.docx -o sample.docx

# Converter
curl -X POST \
  -F "files=@sample.docx" \
  http://localhost/api/convert \
  -o sample.pdf

ls -lh sample.pdf
```

#### Teste 3: Múltiplos Arquivos (sem combine)

```bash
# Criar 2 arquivos
echo "Documento 1" > doc1.txt
echo "Documento 2" > doc2.txt

# Enviar só o primeiro (combine=false, padrão)
curl -X POST \
  -F "files=@doc1.txt" \
  http://localhost/api/convert \
  -o doc1.pdf

echo "✅ OK - Single file"
```

#### Teste 4: Múltiplos Arquivos (com combine)

```bash
# Criar 3 arquivos
echo "Página 1" > page1.txt
echo "Página 2" > page2.txt
echo "Página 3" > page3.txt

# Converter e combinar
curl -X POST \
  -F "files=@page1.txt" \
  -F "files=@page2.txt" \
  -F "files=@page3.txt" \
  "http://localhost/api/convert?combine=true" \
  -o combined.pdf

ls -lh combined.pdf
file combined.pdf
```

#### Teste 5: Imagem para PDF

```bash
# Baixar imagem de teste ou usar uma existente
curl -L https://via.placeholder.com/500x700 -o test-image.jpg

# Converter
curl -X POST \
  -F "files=@test-image.jpg" \
  http://localhost/api/convert \
  -o image.pdf

ls -lh image.pdf
```

#### Teste 6: Arquivo Grande

```bash
# Criar arquivo grande de teste (50MB)
dd if=/dev/zero bs=1M count=50 of=large-file.bin 2>/dev/null

# Tentar converter (provavelmente vai falhar, é normal)
curl -X POST \
  -F "files=@large-file.bin" \
  http://localhost/api/convert \
  -o large.pdf 2>&1 | head -20
```

#### Teste 7: Formato Inválido

```bash
# Enviar arquivo com extensão inválida
echo "Random data" > invalid.xyz

# Isso deve retornar erro 400
curl -X POST \
  -F "files=@invalid.xyz" \
  http://localhost/api/convert \
  -w "\nHTTP Status: %{http_code}\n"
```

### 5️⃣ Verificar Performance

```bash
# Em um terminal, monitorar containers
docker stats

# Em outro terminal, enviar conversões
for i in {1..5}; do
  echo "Conversão $i"
  curl -X POST \
    -F "files=@sample.docx" \
    http://localhost/api/convert \
    -o output-$i.pdf \
    -w "Status: %{http_code}, Tempo: %{time_total}s\n"
done
```

### 6️⃣ Monitorar Logs

```bash
# Todos os logs
docker-compose logs -f

# Só API
docker-compose logs -f api

# Só Gotenberg
docker-compose logs -f gotenberg

# Últimas 50 linhas
docker-compose logs --tail=50 api
```

---

## 🔍 Debugging

### Problema: Conexão recusada

```bash
# Verificar se containers estão rodando
docker-compose ps

# Verificar se portas estão abertas
netstat -tuln | grep -E "3000|80"  # Linux/Mac
netstat -ano | findstr "3000 80"   # Windows

# Ver logs detalhados
docker-compose logs gotenberg | tail -100
```

### Problema: Conversão falhando

```bash
# Ver erro completo
curl -X POST \
  -F "files=@test.txt" \
  http://localhost/api/convert \
  -v  # verbose mode

# Ver logs da API
docker-compose logs api | grep -i error | tail -20

# Ver logs do Gotenberg
docker-compose logs gotenberg | grep -i error | tail -20
```

### Problema: Timeout

```bash
# Aumentar timeout do curl
curl --max-time 60 -X POST \
  -F "files=@large-file.docx" \
  http://localhost/api/convert \
  -o output.pdf

# Ou verificar se Gotenberg está processando
docker-compose logs gotenberg | tail -50
```

### Problema: Out of Memory

```bash
# Ver uso de memória
docker stats

# Aumentar limite em docker-compose.yml
# Adicionar: mem_limit: 2g

# Reiniciar
docker-compose down
docker-compose up -d
```

---

## 📊 Testes de Stress

### Teste de Carga Simples

```bash
# Enviar 10 conversões em paralelo
for i in {1..10}; do
  curl -X POST \
    -F "files=@sample.docx" \
    http://localhost/api/convert \
    -o output-$i.pdf &
done
wait
echo "✅ 10 conversões concluídas"
```

### Teste de Carga com Apache Bench

```bash
# Se tiver arquivo preparado
# Usar ferramenta: apache2-utils (apt install apache2-utils)

# Primeiro, criar dados para POST
# Este é mais complexo - ver seção Advanced

echo "Teste manual recomendado acima"
```

---

## ✨ Script Automático (Recomendado)

Criar arquivo `run-tests.sh`:

```bash
#!/bin/bash

echo "🧪 Iniciando testes..."
echo ""

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

TEST_COUNT=0
PASS_COUNT=0
FAIL_COUNT=0

# Função para testar
test_conversion() {
    local name=$1
    local input=$2
    local expected=$3
    
    echo -n "🧪 Testando $name... "
    TEST_COUNT=$((TEST_COUNT + 1))
    
    curl -X POST \
      -F "files=@$input" \
      http://localhost/api/convert \
      -o test-output.pdf \
      -s -w "%{http_code}" > /tmp/status.txt
    
    local status=$(cat /tmp/status.txt)
    
    if [ "$status" = "200" ] || [ "$status" = "200 " ]; then
        echo -e "${GREEN}✅ PASS${NC}"
        PASS_COUNT=$((PASS_COUNT + 1))
    else
        echo -e "${RED}❌ FAIL (HTTP $status)${NC}"
        FAIL_COUNT=$((FAIL_COUNT + 1))
    fi
}

# Preparar arquivos de teste
cd /tmp
echo "Test content" > test.txt
echo "Test 2" > test2.txt

# Executar testes
echo "📌 Verificando conexão..."
if ! curl -s http://localhost/api/convert/health > /dev/null; then
    echo -e "${RED}❌ API não respondendo!${NC}"
    exit 1
fi
echo -e "${GREEN}✅ API respondendo${NC}"

echo ""
echo "📌 Iniciando testes de conversão..."
test_conversion "TXT simples" "test.txt" "200"
test_conversion "Múltiplos TXT" "test.txt" "200"

# Resumo
echo ""
echo "📊 Resumo dos Testes"
echo "===================="
echo -e "Total: $TEST_COUNT"
echo -e "${GREEN}Passou: $PASS_COUNT${NC}"
echo -e "${RED}Falhou: $FAIL_COUNT${NC}"

if [ $FAIL_COUNT -eq 0 ]; then
    echo -e "\n${GREEN}✅ Todos os testes passaram!${NC}"
    exit 0
else
    echo -e "\n${RED}❌ Alguns testes falharam${NC}"
    exit 1
fi
```

Executar:
```bash
chmod +x run-tests.sh
./run-tests.sh
```

---

## 📝 Checklist Antes do Commit

```bash
# 1. Todos os testes passaram?
./run-tests.sh

# 2. Sem erros nos logs?
docker-compose logs | grep -i error

# 3. Performance OK?
docker stats --no-stream  # Verificar uso de recursos

# 4. Código compilado?
dotnet build backend/Docs2Pdf.Api.csproj

# 5. Sem warnings críticos?
dotnet build backend/Docs2Pdf.Api.csproj /warnaserror

# 6. Limpeza
docker-compose logs --no-stream  # Última verificação
docker-compose down -v             # Limpar para próxima execução

# 7. Commitar
git add .
git commit -m "feat: integração com Gotenberg para conversão mais rápida"
```

---

## 🛑 Parar Tudo

```bash
# Parar containers (mantém volumes)
docker-compose down

# Parar e remover tudo (limpa volumes)
docker-compose down -v

# Parar e remover tudo + imagens
docker-compose down -v --rmi all
```

---

## 💡 Dicas

1. **Use `-d` (detached)** - `docker-compose up -d` para não travar terminal
2. **Monitore em outro terminal** - `docker-compose logs -f` enquanto testa
3. **Limpe entre testes** - `docker-compose down -v` evita conflitos
4. **Teste incrementalmente** - Não faça tudo de uma vez
5. **Guarde exemplos** - Salve arquivos que funcionam/falharam

---

## 🎯 Fluxo Recomendado

```
1. Limpar: docker-compose down -v
2. Iniciar: docker-compose up -d
3. Aguardar: Verificar health (curl http://localhost:3000/health)
4. Testar: ./run-tests.sh ou testes manuais
5. Monitorar: docker stats em outro terminal
6. Debugar: docker-compose logs -f (se houver erros)
7. Validar: Todos os testes passando?
8. Commitar: git commit ...
9. Limpar: docker-compose down -v
```

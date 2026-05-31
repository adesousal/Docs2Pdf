# Docs2Pdf Backend

API .NET 8 para conversão de documentos e imagens em PDF.

## Como usar

1. Entre no diretório do backend:

```bash
cd backend
```

2. Compile o projeto:

```bash
dotnet build
```

3. Execute localmente:

```bash
dotnet run
```

4. O serviço estará disponível em `http://localhost:5000`.

## Docker

Use o `Dockerfile` para criar uma imagem Docker com LibreOffice instalado:

```bash
docker build -t docs2pdf-backend .
```

## Observações de segurança

- O backend valida extensão, MIME type e tamanho de arquivo.
- Os arquivos temporários são gravados com nomes GUID no diretório temporário e limpos após processamento.
- O serviço roda como usuário não-root no container.

# Docs2Pdf Frontend

Este projeto Angular representa a interface web para envio de arquivos e visualização de PDFs.

## Como usar

1. Instale dependências:

```bash
cd frontend
npm install
```

2. Execute em desenvolvimento:

```bash
npm run start
```

3. Construa para produção:

```bash
npm run build
```

## Notas

- A aplicação envia uploads para `http://localhost:5000/api/convert` por padrão.
- Para produção, atualize `ConvertService.apiUrl` para a URL da API hospedada.
- Não há testes automatizados neste projeto.

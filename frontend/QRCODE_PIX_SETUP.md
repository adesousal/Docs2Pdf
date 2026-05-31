# Configuração do QR Code Pix

## Como adicionar seu QR Code Pix

1. **Gere seu QR Code Pix:**
   - Acesse uma ferramenta de geração de QR Code Pix (ex: Banco do Brasil, Nubank, geradores online)
   - Copie o código QR code gerado como imagem PNG

2. **Adicione ao projeto:**
   - Salve a imagem como `pix-qrcode.png`
   - Copie para: `frontend/src/assets/pix-qrcode.png`

3. **Configure a chave Pix:**
   - Abra: `frontend/src/app/components/qrcode-pix/qrcode-pix.component.html`
   - Procure por: `<code>sua-chave-pix.exemplo.com</code>`
   - Substitua pela sua chave Pix real

4. **Teste:**
   - Execute `npm start`
   - Clique no botão "💙 Fazer uma doação" no header
   - Verifique se o QR code e a chave aparecem corretamente

## Segurança

- ✅ QR Code armazenado como arquivo estático (seguro)
- ✅ Chave Pix visível apenas ao clicar no botão (melhor UX)
- ✅ Modal com backdrop que fecha ao clicar fora
- ✅ Nenhuma exposição desnecessária de dados sensíveis

## Boas Práticas Implementadas

1. **QR Code Fixo em Assets**: Armazenado como arquivo PNG na pasta pública
2. **Modal com Backdrop**: Usuário precisa clicar intencionalmente para ver
3. **Fallback Gracioso**: Se a imagem não carregar, mostra placeholder
4. **Responsivo**: Funciona bem em mobile
5. **Acessível**: Botão com título descritivo, cores contrastantes

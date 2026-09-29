# Roteiro R-046 — Tamanho do inventário pelo admin (F4.2a, parte 1)

| Campo | Valor |
|---|---|
| Pacote | `GenesisMods-GenesisUI-0.7.0-preview.2.zip` |
| Tempo estimado | ~15 min |
| Pré-requisito | Mundo local (single player): você é o admin, as opções `[Inventory]` são suas. Faça um backup do personagem de teste antes. |

**O que é testado:** o inventário com 32/40/48 espaços e a segurança dos itens quando o tamanho
muda. **O que não é testado:** o visual do concept (vem na próxima parte), equipamento, consumo
rápido e utilitários (as linhas deles já existem, mas ficam escondidas e vazias).

| # | Faça | Esperado | Se for diferente, anote |
|---|---|---|---|
| 1 | Entre com o personagem de teste e abra o inventário. | 4 linhas (32 espaços), como antes; nenhuma linha vazia a mais aparecendo. | Print `R-046-01.png`. |
| 2 | F1 → `[Inventory] Rows` = **6**. Abra o inventário. | **6 linhas (48 espaços)**. Os itens continuam onde estavam. | Print `R-046-02.png`. |
| 3 | Encha o inventário todo (pegue pedras, madeira...) e continue coletando. | Com as 6 linhas cheias, o jogo diz que o inventário está cheio: **nada some** e nada vai para lugar escondido. Anote a quantidade total de itens (o F8 ajuda) antes e depois. | Anote as contagens. |
| 4 | Esvazie algumas linhas deixando itens nas linhas 5 e 6. F1 → `Rows` = **4**. | Os itens das linhas 5 e 6 descem para espaços livres das 4 primeiras. **Nenhum item no chão, nenhum sumido.** | Print antes/depois. |
| 5 | Encha as 6 linhas de novo e tente `Rows` = **4**. | Mensagem no centro: o novo tamanho não coube, e o inventário **continua com 6 linhas**. | Print da mensagem. |
| 6 | Com `Rows` = 6, saia do mundo e entre de novo; depois morra e pegue a lápide. | Continua com 48 espaços; a lápide devolve tudo. | Anote contagens. |
| 7 | F8: **Injetar falha** em `inv.slots` e **Reativar**. Depois **Gravar relatório**. | Na falha, o inventário mostra também as linhas reservadas (vazias); ao reativar, somem de novo. No relatório: `InventorySizePatch` e `InventoryPlacementPatches` Applied. | Mande o relatório e o `LogOutput.log`. |

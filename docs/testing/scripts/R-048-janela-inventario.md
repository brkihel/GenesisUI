# Roteiro R-048 — Janela do inventário (F4.2a, parte 2)

| Campo | Valor |
|---|---|
| Pacote | `GenesisMods-GenesisUI-0.7.0-preview.4.zip` |
| Tempo estimado | ~15 min |
| Pré-requisito | Backup do personagem de teste. Rode junto os passos do R-047 se ainda não rodou. |

**O que é testado:** a aba Inventário no layout do concept, com os espaços do jogo por cima.
**O que ainda não existe:** os espaços do painel Equipamento, consumo rápido e utilitários
(próximos pacotes), o baú vestido (ele abre, mas com o visual do jogo), Criação no layout do concept.

| # | Faça | Esperado | Se for diferente, anote |
|---|---|---|---|
| 1 | Abra o inventário (Tab). | Três painéis: **Inventário** (grade com os espaços finos, "N/32" no topo, filtro "Todos", barra de **Peso** embaixo), **Equipamento** (Proteção total), **Detalhes do item**. Nada do painel de madeira do jogo aparecendo. | Print `R-048-01.png`. |
| 2 | Passe o mouse nos itens. | **Sem tooltip do jogo**; o painel Detalhes mostra ícone, nome e o texto do item (e fica no último item). | Print. |
| 3 | Arraste, divida (Shift+clique), troque itens de lugar, equipe e desequipe (clique direito). | Tudo funciona **como no jogo**; ícones, quantidades e durabilidade dentro dos espaços. Conte os itens antes/depois de mexer bastante. | Anote contagens. |
| 4 | Clique no filtro várias vezes. | O nome muda (Armas, Armaduras...) e os itens de outras categorias ficam **apagados no lugar**, sem sair do espaço. Arrastar um item apagado funciona. | Print com "Armas". |
| 5 | Pegue itens pesados. | O número de Peso e a barra acompanham; acima do limite a barra fica vermelha. | Anote. |
| 6 | Vá para Habilidades, Criação, Conquistas e volte ao Inventário. | Nas outras abas o inventário do jogo volta ao normal (Criação ainda no visual do jogo); ao voltar, o layout do GenesisUI reaparece. | Anote. |
| 7 | Abra um baú. | O painel do baú aparece no lugar de Equipamento/Detalhes; pegar tudo, arrastar e Ctrl+clique funcionam. | Print. |
| 8 | Com `[Inventory] Rows` = 6, abra o inventário. | 6 linhas na grade, sem sobrar para fora do painel. | Print. |
| 9 | F8: **Injetar falha** em `win.inventory`; **Reativar**. **Gravar relatório**. | Na falha o inventário do jogo volta **inteiro e normal**; ao reativar, o layout volta. | Mande relatório e `LogOutput.log`. |

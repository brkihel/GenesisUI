# Roteiro R-053 — Janela de inventário própria (layout da ConceptArt 9)

| Campo | Valor |
|---|---|
| Pacote | `GenesisMods-GenesisUI-0.7.0-preview.9.zip` |
| Pré-requisito | Perfil `test` com backup do personagem. Anote a quantidade de alguns itens antes. |
| Não testado aqui | Controle (gamepad) no inventário — ainda não ligado às células novas. |

| # | Faça | Esperado | Se for diferente, envie |
|---|---|---|---|
| 1 | Abra o inventário (Tab). | Janela no layout do concept: abas em cima, Inventário (grade 8×4, "x/32 SLOTS EM USO", filtro e Organizar), Equipamento (Cabeça, Peito, Capa, Pernas à esquerda; Trinket, Cinto à direita; Proteção total), Detalhes, e as dicas distribuídas na barra de baixo desde a primeira abertura. Nada do inventário vanilla aparece. | Print. |
| 2 | Compare o print com a ConceptArt (9). | Proporções parecidas: a grade ocupa o painel, consumo rápido e slots de ação logo abaixo, peso no rodapé do painel. | Print marcando o que ficou diferente. |
| 3 | Passe o mouse sobre itens. | Moldura dourada de destaque na célula; Detalhes mostra ícone, nome, tipo, descrição e a tabela (peso, durabilidade com barra, qualidade, dano/armadura, comida, valor). Sem tooltip vanilla. | Print. |
| 4 | Clique num item, clique em outra célula. Depois arraste um item até outra célula. | Clique pega o item (ícone segue o mouse) e o segundo clique solta/troca; arrastar faz o mesmo. Quantidades iguais. | Vídeo curto se falhar. |
| 5 | Shift+clique numa pilha; Ctrl+clique com um baú aberto; botão direito numa comida e numa arma. | Shift abre a divisão de pilha do jogo por cima da janela; Ctrl transfere para o baú; direito come/equipa. | Print. |
| 6 | Pegue um item e clique fora da janela (no mundo). Depois arraste outro item para fora. | O item cai no chão, como no vanilla. | Vídeo. |
| 7 | Arraste uma comida para um slot de Consumo rápido e uma arma para um slot de ação (Z). Tente pôr uma arma no consumo rápido e uma armadura no slot de ação. | Os dois primeiros entram; os proibidos são recusados com a mensagem do jogo, sem sumir item. | Quantidades antes/depois. |
| 8 | Clique com o botão direito numa peça de armadura; depois no item equipado no painel. | Equipa e vai para o slot do painel (moldura de equipado); desequipar volta para a grade. | Print. |
| 9 | Aperte **R** (ou o botão Organizar). | Linhas abaixo da hotbar ficam agrupadas por tipo; hotbar, consumo rápido, ação e equipamento não mudam. Total de itens igual. | Print antes/depois. |
| 10 | Escolha "Armas" no filtro. | Os outros itens ficam esmaecidos no lugar. | Print. |
| 11 | Se o admin tiver 5 ou 6 linhas: role a roda do mouse sobre a grade. | A grade rola e a barrinha à direita acompanha. | Print. |
| 12 | Abra um baú. | Painel do baú aparece no lugar de Equipamento/Detalhes, com Pegar tudo e Empilhar funcionando; arrastar entre baú e inventário funciona. | Vídeo curto. |
| 13 | Feche e abra várias vezes; troque de aba com Q/E. | Sem piscar o inventário vanilla; volta completo. | Vídeo. |

Ao final, grave o relatório no F8 e envie o `.log` do GenesisUI e o `LogOutput.log`
(procure as linhas `Module:win.inventory`).

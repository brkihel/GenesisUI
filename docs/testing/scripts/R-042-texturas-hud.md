# Roteiro R-042 — Suas texturas no HUD inteiro (F4.0)

| Campo | Valor |
|---|---|
| Pacote | `GenesisMods-GenesisUI-0.6.0-preview.1.zip` |
| Tempo estimado | ~20 min |
| Pré-requisito | Configuration Manager instalado (passos 9 e 10) |
| Substitui | R-041 (nunca rodado; as correções dele continuam no pacote) |

**O que é testado:** todas as molduras do HUD agora saem das suas folhas
(`UI_elements1..5`), o fundo de pedra escura fica atrás delas com opacidade própria, e as
três barras (vida, vigor, eitr) usam os seus líquidos animados.
**O que não é testado:** janelas de inventário/criação (F4.1 em diante).

Tire os prints em **1920×1080** se possível, com a escala de interface do jogo no padrão.

| # | Faça | Esperado | Se for diferente, anote |
|---|---|---|---|
| 1 | Entre no mundo e olhe o HUD parado, de dia. | Todas as peças no seu estilo dourado das folhas: barras verticais, comida, barra de itens, minimapa com placa de dia/hora e de bioma, efeitos. Nenhum retângulo liso nem moldura antiga. | Print `R-042-01.png` da tela inteira. |
| 2 | Olhe as **três barras** de perto (canto inferior esquerdo). | Vida maior, vigor médio, eitr menor — **as três proporções que você desenhou**, sem nenhuma esticada. O líquido vermelho, dourado e azul é a sua textura, **se movendo devagar para cima**, sem costura/linha visível passando, sem cantos pretos no topo e na ponta de baixo. | Print `R-042-02.png`. Se aparecer uma linha "pulando", anote em qual barra. |
| 3 | Corra até gastar vigor; tome um dano. | O nível desce e sobe **independente** do movimento do líquido; a borda que queima, as brasas e o brilho da superfície continuam. A barra pequena de vigor aparece acima dos itens com o **número** em cima e o **líquido dourado** no canal de baixo. | Print `R-042-03.png` da barra pequena. |
| 4 | Deixe a vida abaixo de 25 %. | A moldura da vida pulsa em vermelho; o líquido segue normal por dentro. | Anote. |
| 5 | Troque de item com as teclas 1–8 e equipe uma arma. | A barra de itens é a sua peça de 8 células, **inteira e sem distorção**; ícones, números 1–8, quantidades e durabilidade caem **dentro** das células. A célula equipada **brilha por dentro** (sem moldura extra por cima). | Print `R-042-05.png`. Anote se algum ícone sai da célula. |
| 6 | Mire num baú e depois pegue alguns itens do chão. | Cartão de interação com os nós nos cantos e os **losangos laterais inteiros** no meio das bordas; notificações no canto superior esquerdo na placa fina com o losango à esquerda, texto sem encostar nas pontas. | Print `R-042-06.png`. |
| 7 | Chegue perto de uma criatura e (se puder) de um chefe. | Placa da criatura fininha com as pontas de nó; placa do chefe com nó nas pontas e a **vida em líquido vermelho** que se move de lado. | Prints `R-042-07a.png` (criatura) e `R-042-07b.png` (chefe, se houver). |
| 8 | Olhe o minimapa. | Anel redondo fino com 4 losangos; o mapa preenche o anel **sem vazar** para fora e sem faixa vazia na borda; placa de dia/hora com o losango no topo inteiro; placa do bioma embaixo. | Print `R-042-08.png`. |
| 9 | F1 → `[Backgrounds] Default` = **0**, depois **1**, depois volte a **0.85**. | Em 0 o fundo de pedra some de **todas** as molduras, mas o dourado, os textos e os ícones ficam iguais. Em 1 fica opaco. Muda na hora, sem reiniciar. | Print em 0 e em 1 (`R-042-09a/b.png`). |
| 10 | F1 → `[Backgrounds] Hotbar` = **0**. | Só a barra de itens perde o fundo; o resto continua. Volte para **-1**. | Anote. |
| 11 | Abra o F8. | Linha **Art: N sprites, M backgrounds** perto do fim. Todos os módulos `Active`. **Injetar falha** em `hud.vitals` e **Reativar**: as barras voltam normais, com líquido. | Print do F8. |
| 12 | **Gravar relatório** no F8. | — | Me mande o relatório e o `LogOutput.log`. |

**Opinião (o mais importante desta rodada):** o líquido está bonito e sutil ou precisa ser
mais claro/mais rápido/mais devagar? O fundo de pedra a 0.85 está bom? Alguma peça ficou
pequena ou grossa demais em relação às outras (principalmente o anel do minimapa e o medalhão
embaixo da barra de vida)?

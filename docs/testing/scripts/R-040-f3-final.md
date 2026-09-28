# Roteiro R-040 — Fechamento da F3: madeira cravada, barras vivas e o HUD completo

| Campo | Valor |
|---|---|
| Pacote | `GenesisMods-GenesisUI-0.5.0-preview.1.zip` |
| Tempo estimado | ~30 min |
| Pré-requisito | R-000 no mesmo pacote |

## O que estamos testando

O novo estilo **madeira cravada** (`carved`) em todo o HUD e a troca ao vivo para o
dourado (`gold`); os efeitos novos das barras (veios, queima com brasas, placa do número,
brilho de perigo); a barra de vigor pequena; e as peças que fecham a F3: placa do chefe,
cartão de interação, notificações e dicas de atalho acima da barra de itens.

Este é o review final da F3: além de passar/falhar, diga qual direção de arte prefere.

## O que NÃO estamos testando

- Placas de inimigos comuns (continuam as do jogo), posicionamento pelo jogador, mods
  de terceiros, desempenho com o modpack (fica para depois do review).

## Preparação

1. Perfil local de teste com BepInExPack, Jötunn e este pacote; outros mods desligados.
2. Apague o `BepInEx/config/Genesis.GenesisUI.cfg` antigo para receber os padrões novos
   (ou confira `[Theme] Style = carved`).
3. Mundo local de teste, com `devcommands` disponível no console (F5).

## Passos

| # | Faça | Esperado | Se for diferente, anote |
|---|---|---|---|
| 1 | Entre no mundo e olhe o HUD inteiro parado. | Tudo em **madeira escura cravada com rebites de ferro**: barras com capitel com o **V** e pomo em losango, comida, barra de itens numa viga, efeitos de status, minimapa com aro de madeira. Uma família só, nada dourado sobrando. | Print `R-040-01.png`. Diga o que destoa. |
| 2 | Olhe as barras de perto por uns 20 s. | Dentro do líquido, **veios** claros e manchas escuras se movendo devagar (sangue na vida, na cor de cada barra); bolhas discretas; o número numa plaquinha perto da base, legível. | Print `R-040-02.png`; diga se está forte ou fraco. |
| 3 | Leve dano (queda de uma altura, ou `damage 30` se preferir) e gaste vigor com um golpe. | A parte perdida **queima**: faixa na cor quente da barra (laranja na vida, dourado claro no vigor), densa na superfície e se desfazendo para cima, com **brasas** subindo; some em ~1 s. | Print `R-040-03.png` no meio do efeito. |
| 4 | Deixe a vida abaixo de 25 % (use `damage` até lá). | Um **halo suave pulsa** em volta da barra de vida e brasas saem das laterais; volta ao normal ao curar. | Print `R-040-04.png`. |
| 5 | Barra de vigor: **1)** corra 2 s e pare; **2)** pule; **3)** dê um golpe; **4)** fique parado até encher. | A barra **pequena** (bem menor que antes) aparece acima dos itens em **qualquer** gasto de vigor, não só correndo, e só some **depois do vigor totalmente cheio**, suavemente. | Print `R-040-05.png` e anote algum caso em que não apareceu. |
| 6 | Olhe para um baú, uma porta e um item no chão. | Um **cartão** de madeira ao lado da mira com o nome (dourado) e as ações (`[E] Abrir`…); o texto original do jogo não aparece por trás. | Print `R-040-06.png`. |
| 7 | Pegue alguns itens do chão; depois durma numa cama ou pise num lugar que mostre mensagem central (ex.: entrar num bioma novo). | Canto superior esquerdo: **cartão** com ícone e texto (`Madeira x5`), sumindo como o do jogo. Mensagens centrais em letra grande no centro-alto. | Print `R-040-07.png`. |
| 8 | Observe as **dicas de atalho** do jogo (as teclas na parte de baixo, ex.: segurando um martelo ou arco). | Ficam **acima** da barra de itens, sem sobrepor a viga. | Print `R-040-08.png`. |
| 9 | No console: `devcommands`, depois `spawn Eikthyr`. Afaste-se um pouco e volte. | Placa do chefe **no topo, ao centro**: nome, vida com número e rastro quente quando ele perde vida. A barra de chefe do jogo não aparece. (Mate-o ou use `killall` no fim.) | Print `R-040-09.png`. |
| 10 | Com o jogo aberto, `F1` (Configuration Manager) → `[Theme] Style` → `gold`. Depois volte para `carved`. | O HUD troca **na hora** para o dourado e volta, sem reiniciar e sem peça faltando. | Print `R-040-10.png` no dourado. |
| 11 | No F8, **Injetar falha** em `hud.boss`, `hud.hover` e `hud.notice` (um de cada vez) e **Reativar**. | Com a falha, volta a peça do jogo correspondente (as dicas de atalho voltam ao lugar original se a falha for em `hud.hotbar`); ao reativar, a nossa volta. | Anote o que não voltou. |
| 12 | No fim, **Gravar relatório** no F8. | Seção **Theme** com `style: carved` e todos os módulos `Active`, sem falha inesperada. | Me mande o relatório e o `LogOutput.log`. |

## Critério de aprovação

Todos os passos esperados confirmados e nenhuma falha inesperada do GenesisUI no log.
A F3 fecha com o seu review; a F4 começa depois dele.

## O que me enviar

O relatório do F8, `BepInEx/LogOutput.log`, os prints, e sua escolha de direção de
arte (madeira cravada, dourado, ou os dois como opção) com o que mudaria em cada um.

# Roteiro R-040 — Fechamento da F3: o HUD completo

| Campo | Valor |
|---|---|
| Pacote | `GenesisMods-GenesisUI-0.5.0-preview.3.zip` |
| Tempo estimado | ~30 min |
| Pré-requisito | R-000 no mesmo pacote |

## O que estamos testando

A F3 inteira no visual dourado (a única direção de arte agora): barras com o líquido novo
(rachaduras finas, queima com brasas, plaquinha do número opcional, brilho de perigo),
barra de vigor pequena, placa do chefe, **placas das criaturas** (novas), cartão de
interação, notificações e dicas de atalho acima da barra de itens.

## O que NÃO estamos testando

- Posicionamento pelo jogador, mods de terceiros, desempenho com o modpack (vem depois
  do seu review).

## Preparação

1. Perfil local de teste com BepInExPack, Jötunn e este pacote; outros mods desligados.
2. Apague o `BepInEx/config/Genesis.GenesisUI.cfg` antigo para receber os padrões novos
   (a opção `[Theme] Style` não existe mais).
3. Mundo local de teste, com `devcommands` disponível no console (F5).

## Passos

| # | Faça | Esperado | Se for diferente, anote |
|---|---|---|---|
| 1 | Entre no mundo e olhe o HUD inteiro parado. | Tudo no dourado, uma família só: barras, comida, barra de itens, efeitos, minimapa. Nada de madeira sobrando. | Print `R-040-01.png`. Diga o que destoa. |
| 2 | Olhe as barras de perto por uns 20 s. | Líquido calmo: nuvens escuras suaves e **poucas rachaduras finas e compridas** subindo devagar, na cor de cada barra; bolhas discretas. O número numa plaquinha perto da base, só um pouco mais larga que a barra. | Print `R-040-02.png`; diga se está forte ou fraco. |
| 3 | No F1 (Configuration Manager), `[Vitals] ShowValues = false`; depois volte para `true`. | Sem números as barras ficam limpas; com números a plaquinha volta. Na hora, sem reiniciar. | Anote. |
| 4 | Leve dano (uma queda, ou deixe uma criatura do passo 9 bater) e gaste vigor com um golpe. | A parte perdida **queima**: faixa na cor quente da barra (laranja na vida, dourado claro no vigor), densa na superfície e se desfazendo para cima, com **brasas**; some em ~1 s. | Print `R-040-04.png` no meio do efeito. |
| 5 | Deixe a vida abaixo de 25 %. | Um **halo suave pulsa** em volta da barra de vida e brasas saem das laterais; volta ao normal ao curar. | Print `R-040-05.png`. |
| 6 | Barra de vigor: **1)** corra 2 s e pare; **2)** pule; **3)** dê um golpe; **4)** fique parado até encher. | A barra pequena aparece acima dos itens em **qualquer** gasto de vigor e só some **depois do vigor totalmente cheio**, suavemente. | Print `R-040-06.png` e anote algum caso em que não apareceu. |
| 7 | Olhe para um baú, uma porta e um item no chão. | **Cartão** dourado ao lado da mira com o nome (dourado) e as ações (`[E] Abrir`…); o texto original do jogo não aparece por trás. | Print `R-040-07.png`. |
| 8 | Pegue alguns itens do chão; depois faça aparecer uma mensagem central (ex.: dormir numa cama ou entrar num bioma novo). | Canto superior esquerdo: **cartão** com ícone e texto, sumindo como o do jogo. Mensagem central em letra grande no centro-alto. | Print `R-040-08.png`. |
| 9 | No console: `devcommands`, depois `spawn Greydwarf 1 1`, `spawn Greydwarf 1 2` e `spawn Greydwarf 1 3`. Deixe que te vejam e lute com eles; depois passe a mira num cervo ou galinha sem atacar. | Sobre cada criatura, uma **placa pequena** no estilo da placa do chefe: nome, vida (com rastro quente ao perder), **0, 1 e 2 estrelas** conforme o nível; um **"?" dourado** quando ela te percebe e um **"!" vermelho** quando está em alerta. As placas do jogo não aparecem por trás. Ela some quando o jogo sumiria a dele (longe ou um tempo depois de mirar). | Print `R-040-09.png` com as três. Anote se a placa ficou alta/baixa demais em relação à cabeça (ajuste em `[Enemy] OffsetY`). |
| 10 | Mate as criaturas. | As placas somem junto, sem sobras na tela. | Anote. |
| 11 | Observe as **dicas de atalho** do jogo (as teclas embaixo, ex.: segurando um martelo ou arco). | Ficam **acima** da barra de itens, sem sobrepor a moldura. | Print `R-040-11.png`. |
| 12 | No console: `spawn Eikthyr`. Afaste-se um pouco e volte. | Placa do chefe **no topo, ao centro**: nome, vida com número e rastro quente quando ele perde vida. A barra de chefe do jogo não aparece, e não surge placa pequena sobre ele. (Use `killall` no fim.) | Print `R-040-12.png`. |
| 13 | No F8, **Injetar falha** em `hud.enemy`, `hud.boss`, `hud.hover` e `hud.notice` (um de cada vez, com a peça na tela) e **Reativar**. Faça o mesmo em `hud.hotbar`. | Com a falha, volta a peça do jogo correspondente (em `hud.hotbar`, as dicas de atalho voltam ao lugar original); ao reativar, a nossa volta. | Anote o que não voltou. |
| 14 | No fim, **Gravar relatório** no F8. | Todos os módulos `Active`, sem falha inesperada; na lista de véus, uma linha `hud.enemy: N creature plate(s)` enquanto houver criaturas. | Me mande o relatório e o `LogOutput.log`. |

## Critério de aprovação

Todos os passos esperados confirmados e nenhuma falha inesperada do GenesisUI no log.
A F3 fecha com o seu review; a F4 começa depois dele.

## O que me enviar

O relatório do F8, `BepInEx/LogOutput.log`, os prints e o que mudaria no acabamento.

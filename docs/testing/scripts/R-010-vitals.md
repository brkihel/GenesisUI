# Roteiro R-010 — Barras de vida, vigor e eitr

| Campo | Valor |
|---|---|
| Pacote | `GenesisMods-GenesisUI-0.2.0-preview.1.zip` |
| Tempo estimado | ~20 min |
| Pré-requisito | R-000 (passou na 0.1.0) |

## O que estamos testando

Que o **primeiro módulo visual** (as barras verticais de vida, vigor e eitr no canto
inferior esquerdo) aparece com a arte e as fontes do GenesisUI, **acompanha os valores do
jogo**, esconde as barras originais sem quebrá-las, e **devolve as barras originais**
quando é desligado ou falha.

## O que NÃO estamos testando

- O resto do HUD (barra de itens, minimapa, comida, efeitos): continua o do jogo.
- Posição final e acabamento da arte: é a primeira versão, e a sua opinião sobre o
  visual é bem-vinda no fim do roteiro.
- Servidor e multiplayer.

## Preparação

1. No perfil de teste, **remova** a 0.1.0 e instale o zip novo (mesmo jeito do R-000).
   Dentro de `BepInEx/plugins/GenesisMods-GenesisUI/` devem existir: `GenesisUI.dll`,
   `Translations/`, `fonts/` (5 fontes + 2 licenças) e `art/` (3 imagens + `sprites.json`).
2. Use o mundo `GenesisUI-Teste` e o personagem de teste.
3. Para o passo de vida baixa, um lugar alto para pular ajuda (penhasco ou telhado).

## Passos

| # | Faça | Esperado | Se for diferente, anote |
|---|---|---|---|
| 1 | Abra o jogo e vá até o menu principal. | Marca d'água `GenesisUI PREVIEW 0.2.0-preview.1+…` no canto inferior direito. | Print `R-010-01.png`. |
| 2 | Entre no mundo. Olhe o canto **inferior esquerdo**. | Duas barras verticais com moldura dourada em arco e um losango no topo: **vida (vermelha, mais alta)** e **vigor (dourada)**, com o número dentro de cada uma, e o medalhão com o nó embaixo da vida. A barra de eitr **não** aparece (personagem sem eitr). As barras originais do jogo **não** aparecem. | Print `R-010-02.png` da tela inteira. |
| 3 | Compare o número da vida com o valor real: abra o inventário (**Tab**) e veja a vida no painel do personagem. | Os números batem. | Anote os dois valores. |
| 4 | Corra (**Shift**) até o vigor cair e pare. | A barra dourada desce enquanto corre, sobe quando para, e o número acompanha. A barra **continua visível** mesmo cheia (no jogo original ela some). | Anote se travou ou piscou. |
| 5 | Leve dano: pule de um lugar alto ou deixe um javali bater. | A vida cai na hora, e por um instante aparece um **rastro mais claro** do que foi perdido, que depois desce até o valor novo. | Print `R-010-05.png` logo após o dano, se conseguir. |
| 6 | Continue levando dano até ficar com **menos de 1/4 da vida**. | A moldura da barra de vida **pulsa devagar em tom de brasa**. Ao se curar acima de 1/4, o pulso para. | Anote se não pulsou. |
| 7 | Aperte **Ctrl+F3** (esconder HUD). Depois aperte de novo. | As barras somem junto com o resto do HUD e voltam junto. | Anote se ficaram na tela. |
| 8 | Abra o mapa (**M**), feche; abra o menu (**Esc**), feche. | Nada fica duplicado nem fora do lugar. | Print se algo mudar. |
| 9 | Aperte **F8**. | Abre o **painel de diagnóstico** no canto superior esquerdo, com título em dourado, e o mouse fica livre. Em *Módulos*: `hud.vitals: Active` com um tempo em `ms/refresh`. Em *Regiões*: `hud.health`, `hud.stamina`, `hud.eitr` → `module:hud.vitals`. | Print `R-010-09.png` do painel. |
| 10 | No painel, clique **Mostrar vanilla**. Depois **Esconder vanilla**. | Com "mostrar", as barras originais do jogo aparecem **por baixo** das nossas; com "esconder", somem de novo. | Print `R-010-10.png` com o vanilla à mostra. |
| 11 | No painel, na linha **Vitais**, clique **Injetar falha**. | As nossas barras **somem** e as **barras originais do jogo voltam**. O estado vira `Faulted`, e em *Falhas* aparece `module:hud.vitals x1: InvalidOperationException: Fault injected…`. O resto do jogo segue normal. | Print `R-010-11.png`. |
| 12 | Clique **Reativar** na mesma linha. | As nossas barras voltam, as originais somem, e o estado volta a `Active`. | Anote o que aconteceu. |
| 13 | Clique **Gravar relatório**, depois **Fechar (F8)**. | Aparece a mensagem verde de relatório gravado. Ao fechar, você volta a controlar o personagem normalmente. | Anote se o personagem não respondeu. |
| 14 | Saia para o menu principal e entre de novo no mundo. | As barras aparecem **uma vez só**, no mesmo lugar. | Anote se duplicou. |
| 15 | *(Opcional, se tiver como)* Coma uma comida que dê eitr. | Aparece a **terceira barra, azul**, à direita do vigor. | Anote. |

## Critério de aprovação

- Passos 1 a 14 com o "Esperado" confirmado, e
- nenhuma linha `[Error]` de `GenesisUI` no `LogOutput.log`, **exceto** a da falha
  injetada de propósito no passo 11.

## O que me enviar

1. O relatório do passo 13 (`BepInEx/GenesisUI/reports/report-….log`).
2. `BepInEx/LogOutput.log` e o log de `BepInEx/GenesisUI/logs/`. Neles, procure e me conte
   se aparecem as linhas `[GenesisUI:Host] HUD root under …` e `[GenesisUI:Veil] …`: elas
   dizem como o HUD do jogo é montado e se o jogo "brigou" com o véu de alguma barra.
3. Os prints com os nomes indicados.
4. **A sua opinião sobre o visual**: tamanho, posição, cores, fontes, o que ficou
   diferente da concept-art e deveria mudar. Posição e tamanho podem ser ajustados em
   `BepInEx/config/Genesis.GenesisUI.cfg`, seção `[Vitals]` (`OffsetX`, `OffsetY`,
   `Scale`), com o jogo fechado.

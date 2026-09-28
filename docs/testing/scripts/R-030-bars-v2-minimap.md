# Roteiro R-030 — Barras v2, poder do guardião, restos do vanilla e minimapa redondo

| Campo | Valor |
|---|---|
| Pacote | `GenesisMods-GenesisUI-0.4.0-preview.1.zip` |
| Tempo estimado | ~20 min |
| Pré-requisito | R-020 (aprovado na 0.3.0) |

## O que estamos testando

1. **Barras v2**: moldura mais delicada e o "líquido vivo" com o padrão subindo devagar.
2. **Poder do guardião num quadro só**, com o "x" vermelho na recarga.
3. Que os **restos do vanilla embaixo das barras** sumiram (3 espaços de comida vazios, o
   emblema vermelho e o tracinho dourado).
4. O **minimapa redondo** no canto superior direito, com vento e dia/hora na coroa de cima
   e o bioma embaixo.

## O que NÃO estamos testando

- O mapa grande (M): continua o do jogo.
- As dicas de atalho atrás da barra de itens: ficam para depois.

## Preparação

1. Substitua o pacote no perfil de teste (a pasta `art/` agora tem 15 imagens + `sprites.json`).
2. Mundo `GenesisUI-Teste`, personagem com o poder de Eikthyr, se possível.

## Passos

| # | Faça | Esperado | Se for diferente, anote |
|---|---|---|---|
| 1 | Entre no mundo e olhe as barras de perto. | Moldura mais fina, com **pequenas volutas nos ombros do arco**, pontos dourados e ponta de estandarte com uma conta embaixo. Dentro, a barra parece **um líquido**: brilho de vidro, uma linha clara na superfície e **veios e faíscas subindo devagar** (vigor um pouco mais rápido; eitr com um brilho que cruza em sentido contrário). | Print `R-030-01.png` bem de perto. |
| 2 | Olhe **embaixo das barras** com atenção. | **Nenhum** espaço de comida vazio, emblema vermelho ou tracinho dourado do jogo original. | Print `R-030-02.png`. |
| 3 | Leve dano e corra. | O líquido desce e sobe suave; o rastro de dano continua funcionando; o padrão não "estica" quando a barra muda. | Anote. |
| 4 | Olhe o **canto superior direito**. | O **minimapa redondo** com anel dourado e losangos nos pontos cardeais, um **N** no topo, a **coroa** em cima do anel com a **seta do vento** e **"Dia N · HH:MM"**, e a **faixa com o nome do bioma** embaixo. O minimapa quadrado do jogo não aparece. | Print `R-030-04.png`. |
| 5 | Ande pelo mundo. | O mapa acompanha você, sem atraso visível; a seta do jogador gira com a câmera; áreas não exploradas continuam **escuras** (o mapa nunca revela nada a mais). | Anote atraso ou tremida. |
| 6 | Coloque um marcador no mapa grande (M, clique duplo), feche e volte até perto dele. | O marcador aparece **dentro do círculo** do minimapa, no lugar certo, e some na borda do círculo. | Print `R-030-06.png`. |
| 7 | Use as teclas de zoom do minimapa, se tiver (ou a roda com o mapa pequeno, conforme sua configuração). | O nosso minimapa segue o zoom do jogo. | Anote. |
| 8 | Observe o relógio da coroa por um minuto de jogo e a seta do vento. | A hora avança; a seta do vento muda de direção junto com o vento e fica mais clara ou mais forte conforme a intensidade. | Anote. |
| 9 | Troque de bioma (vá até a floresta negra ou a água). | O nome na faixa muda. | Anote. |
| 10 | Abra o mapa grande (**M**) e feche. | Com o mapa grande aberto, o nosso minimapa some; ao fechar, volta. | Anote. |
| 11 | Ative o **poder do guardião**, espere o efeito acabar e acompanhe a recarga. | **Um único quadro**: pronto = moldura dourada fixa; ativo = moldura dourada **pulsando** com o tempo do efeito; depois = ícone escurecido, **"x" vermelho pequeno no canto** e o tempo da recarga. Nunca dois quadros do Eikthyr. | Print `R-030-11.png` de cada estado, se der. |
| 12 | No F8, **Injetar falha** em `hud.minimap` e **Reativar**. | Com a falha, o minimapa quadrado do jogo volta; ao reativar, o redondo volta. | Anote. |
| 13 | Com o jogo aberto, no fim, clique **Gravar relatório** no F8. | Na seção **Vanilla health panel**, a árvore do painel de vida do jogo com `[veiled]` nos itens escondidos. No log, uma linha `[GenesisUI:Module:hud.minimap] map shader '…' supports stencil: round mask` (ou o aviso de que não suporta). | Me mande o relatório. |

## Critério de aprovação

Passos 1 a 13 com o "Esperado" confirmado, e nenhuma linha `[Error]` de `GenesisUI` fora a
falha injetada.

## O que me enviar

1. O relatório do passo 13, o `LogOutput.log` e o log de `BepInEx/GenesisUI/logs/`.
2. Os prints.
3. Sua opinião sobre as barras v2 e o minimapa. Posição e tamanho do minimapa ficam em
   `[Minimap]` (`OffsetX`, `OffsetY`, `Scale`) no config.

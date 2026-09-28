# Roteiro R-031 — Coesão da arte, bolhas, vento e as duas correções

| Campo | Valor |
|---|---|
| Pacote | `GenesisMods-GenesisUI-0.4.1-preview.1.zip` |
| Tempo estimado | ~15 min |
| Pré-requisito | R-030 |

## O que estamos testando

1. As duas correções: **minimapa não fica mais cinza**; **o relógio do poder do guardião
   some** quando a recarga é zerada.
2. A arte revisada: peças que agora **falam a mesma língua**, bolhas sutis nas barras, vidro
   mais suave, seta do vento num disco próprio, textos que não encostam nas molduras.

## Passos

| # | Faça | Esperado | Se for diferente, anote |
|---|---|---|---|
| 1 | Entre no mundo e olhe o HUD inteiro por um tempo. | As molduras parecem **uma família**: mesma linha dourada nos "recipientes" (barras, placa, anel, coroa, faixa), mesma borda bronze com fio dourado nas "células" (espaços, quadros de efeito). A placa da barra de itens tem **volutas e contas** nas pontas, como as barras e a coroa. | Print `R-031-01.png` da tela inteira. |
| 2 | Olhe as barras de perto. | **Bolhas pequenas subindo devagar**, balançando de leve, bem sutis. Brilho de vidro suave, **sem a faixa branca dura**. | Print de perto `R-031-02.png`. |
| 3 | Deixe a vida com 3 dígitos e o vigor com 3 dígitos. | Os números **cabem dentro das barras** sem encostar na moldura (encolhem um pouco se precisar). | Anote. |
| 4 | Olhe a coroa do minimapa. | A seta do vento fica num **pequeno disco** à esquerda do "Dia · hora", bem visível; com vento forte ela fica um pouco maior. | Print `R-031-04.png`. |
| 5 | Jogue normalmente por **10 minutos ou mais**, andando bastante e abrindo o mapa grande algumas vezes. | O mapa do minimapa **nunca fica cinza**. No log, uma linha `following vanilla's map material 'minimap(Clone)…'` logo no começo, e **nenhum** erro sobre `_zoom`, `_pixelSize` ou `_mapCenter`. | Anote a hora se ficar cinza. |
| 6 | Use o poder do guardião, espere o efeito acabar e **zere a recarga** pelo console. | O quadro volta para "pronto" (dourado) e o relógio **some**. | Anote. |
| 7 | No fim, **Gravar relatório** no F8. | — | Me mande o relatório e o log. |

## O que me enviar

Relatório, `LogOutput.log`, prints, e sua opinião sobre a coesão agora.

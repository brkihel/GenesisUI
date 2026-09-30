# Roteiro R-059 — Autorrecuperação, menu Esc, dicas, mapa e marcadores

| Campo | Valor |
|---|---|
| Pacote | `GenesisMods-GenesisUI-0.8.0-preview.2.zip` |

| # | Faça | Esperado | Envie |
|---|---|---|---|
| 1 | F8 → injete falha em `win.inventory` com o inventário aberto. | O inventário fecha (nada vanilla aparece), surge o popup de desculpas com o erro; "Copiar erro" copia; "Reportar" apagado. Reabra: janela GenesisUI normal. | Print do popup. |
| 2 | Repita a injeção 4 vezes seguidas no mesmo módulo. | Nas 3 primeiras ele volta sozinho; na 4ª o popup diz que ficou desligado. | — |
| 3 | Esc no jogo. | Fundo desfocado (ou escurecido), opções à esquerda na nossa fonte, sem molduras; ao passar o mouse a opção desliza, acende em dourado e aparece um losango. Sair pede confirmação no mesmo estilo. Se a tela ficar preta: `[Theme] MenuBlur = false`. | Print. |
| 4 | Olhe as dicas de atalho (Atacar, Bloquear...). | Fonte e teclas do GenesisUI, ocupando menos da metade da largura, no canto inferior direito. | Print. |
| 5 | Martelo → menu. | Peças que não dá para construir bem apagadas. | Print. |
| 6 | Abra o mapa. | Mapa dentro de um painel como as outras janelas; filtros no topo; paleta em duas colunas à direita com os marcadores novos; interruptores e zoom embaixo. | Print. |
| 7 | Escolha "Minério" na paleta, dê duplo clique no mapa, digite um nome e Enter. | O marcador aparece com o ícone de minério e o nome sem a etiqueta, no mapa grande e no minimapa. Saia e entre no mundo: continua lá. | Print. |
| 8 | Use E/Q para passar pelo Mapa até a Criação. | Criação abre normal. Se cair, o popup mostra o erro: copie e me mande. | Texto do erro. |

Ao final, grave o relatório no F8 e envie-o com o `LogOutput.log`.

# Roteiro R-045 — Moldura das janelas (F4.1)

| Campo | Valor |
|---|---|
| Pacote | `GenesisMods-GenesisUI-0.7.0-preview.1.zip` |
| Tempo estimado | ~10 min |
| Pré-requisito | Configuration Manager (passo 7). Rode junto com R-043 e R-044 se ainda não rodou. |

**O que é testado:** a barra de abas em cima e a barra de atalhos embaixo, com o inventário
aberto. **O que não é testado:** o inventário em si continua o do jogo (ele ganha o layout do
concept na F4.2); Criação ainda só marca a aba; Configurações mostra um aviso.

| # | Faça | Esperado | Se for diferente, anote |
|---|---|---|---|
| 1 | Abra o inventário (Tab). | Surgem, com um fade curto, a **barra de cima** (GENESISUI, tecla Q, seis abas com ícone, tecla E) e a **barra de dicas** embaixo (Esc Fechar, mouse Mover, mouse Usar/Equipar, Shift+mouse Dividir pilha, Ctrl+mouse Transferir, Q/E Abas). **Inventário** aceso. | Print `R-045-01.png`. |
| 2 | Clique em **Habilidades**, depois **Conquistas**. | Abrem as janelas do jogo; a aba clicada fica acesa com a linha fina embaixo. Esc fecha a janela do jogo e a aba volta para Inventário. | Print de cada uma. |
| 3 | Aperte **Q** e **E** várias vezes. | As abas andam para a esquerda/direita; **E não fecha o inventário** enquanto a moldura está aberta. Passando por Mapa, o inventário fecha e abre o **mapa grande** do jogo. | Anote se E fechar o inventário. |
| 4 | Clique em **Configurações**. | Um quadro no centro avisa que as configurações do GenesisUI chegam numa próxima etapa. | Print. |
| 5 | Feche com Tab, Esc e com o botão de voltar do controle (se tiver). | A moldura some junto com o inventário, sem ficar nada na tela. | Anote. |
| 6 | Abra um baú (E no baú). | A moldura aparece igual; o baú funciona como no jogo (pegar tudo, arrastar). | Anote. |
| 7 | F1 → `[Backgrounds] Windows` = 0 e depois 1. `[Windows] NextTabKey` = R. | O fundo das barras some/aparece sem mexer no dourado. Com R como próxima aba, E volta a fechar o inventário (como no jogo) (as teclas desenhadas nas barras só mudam depois de reiniciar o jogo). Volte para E. | Anote. |
| 8 | F8: **Injetar falha** em `win.shell` e **Reativar**. Depois **Gravar relatório**. | Ao injetar, a moldura some e E volta a fechar o inventário; ao reativar, tudo volta. No relatório, em Patches: `InventoryTabKeyPatch` Applied. | Mande o relatório e o `LogOutput.log`. |

**Opinião:** tamanho dos ícones e textos das abas, espessura das barras, o título GENESISUI.

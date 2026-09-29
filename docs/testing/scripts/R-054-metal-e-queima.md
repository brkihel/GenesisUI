# Roteiro R-054 — Molduras finas de metal, queima como luz, tema escuro

| Campo | Valor |
|---|---|
| Pacote | `GenesisMods-GenesisUI-0.7.0-preview.10.zip` |
| Pré-requisito | Perfil `test`. Instale o pacote inteiro (inclui `plugins/art/genesisui.shaders`). |

| # | Faça | Esperado | Se for diferente, envie |
|---|---|---|---|
| 1 | Entre no mundo. Olhe a hotbar. | Fileira de slots finos (a minimalista), sem a moldura grossa. Selecionado com moldura externa e losangos; equipado com um losango no topo, nada verde. | Print. |
| 2 | Abra o inventário. | Molduras em traço fino, cor de metal (bronze a ouro velho), com luz de cima à esquerda; fundo de pedra bem mais escuro; o mundo escurece atrás. | Print. |
| 3 | Fique olhando a janela uns 10 s. | De tempos em tempos um brilho suave atravessa as linhas na diagonal. | Vídeo curto se não aparecer. |
| 4 | Corra até gastar o vigor; leve dano. | Na barra horizontal e nas verticais, a perda vira luz: ponto branco-quente na borda, halo laranja que vaza um pouco da moldura, rastro que esfria e faíscas. Sem "bolhinhas subindo". | Vídeo curto. |
| 5 | Em `[Theme]`, ponha `MetalShader = false` e reinicie o jogo. | Mesmas molduras finas, sem o brilho que passa (versão pintada). Nada rosa ou quebrado. | Print. |
| 6 | Volte `MetalShader = true`. | Volta o metal com brilho. | — |

Ao final, grave o relatório no F8 e envie o `LogOutput.log`; procure a linha
`Theme: shaders: metal on, burn on (Direct3D11)` (ou o nome da API que aparecer).

# Roteiro R-055 — Pegar itens, consumo rápido, ação e reativação

| Campo | Valor |
|---|---|
| Pacote | `GenesisMods-GenesisUI-0.7.0-preview.11.zip` |
| Pré-requisito | Perfil `test`. Anote a quantidade de alguns itens. |

| # | Faça | Esperado | Se for diferente, envie |
|---|---|---|---|
| 1 | Abra o inventário e clique numa comida (pilha de 1 e de várias). | O ícone segue o mouse, com a quantidade quando for mais de 1. A janela não cai. | Relatório F8. |
| 2 | Leve a comida a um slot de Consumo rápido (clique ou arraste). | Entra no slot, mostrando "x/máx". | Relatório F8. |
| 3 | Leve uma arma, um escudo e uma ferramenta para os slots de ação (Z/X/C/V). | Entram. Armadura e comida nos slots de ação são recusadas com a mensagem do jogo. | Quantidades antes/depois. |
| 4 | Pegue um item e clique fora da janela, no mundo. | O item cai no chão, como no vanilla. | — |
| 5 | No F8, injete uma falha em `win.inventory` e reative o módulo. Abra o inventário. | A janela volta completa: equipamento nos slots, consumo rápido e ação aceitando itens. | Relatório F8. |
| 6 | Abra um baú, feche, abra outro de tamanho diferente. | Painel do baú com o tamanho certo em cada um. | Print se errado. |

Ao final, grave o relatório no F8 e envie-o, com o `LogOutput.log` se algo cair.

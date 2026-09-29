# Roteiro R-052 — Primeira abertura e fechamento do inventário

| Campo | Valor |
|---|---|
| Pacote | `GenesisMods-GenesisUI-0.7.0-preview.8.zip` |
| Pré-requisito | Perfil de teste com backup do personagem. |

| # | Faça | Esperado | Se for diferente, envie |
|---|---|---|---|
| 1 | Entre no mundo e abra o inventário **pela primeira vez**. | As seis dicas de atalho ficam distribuídas na barra inferior desde o primeiro quadro, sem se sobrepor. | Vídeo curto ou print da primeira abertura. |
| 2 | Feche e abra o inventário várias vezes, inclusive rapidamente. | A janela vanilla não aparece durante o fechamento; a janela GenesisUI volta completa ao reabrir. | Vídeo de dois ciclos. |
| 3 | Troque de aba com Q/E, equipe e desequipe uma peça; depois injete uma falha em `win.inventory` no F8 e reative. | Equipamento e slots continuam clicáveis. Durante a falha, o inventário vanilla volta por inteiro; após reativar, o tema volta sem destaques presos. | Quantidades antes/depois e print se divergir. |

Ao final, grave o relatório no F8 e envie o `.log` e `LogOutput.log` se ocorrer qualquer
divergência. O roteiro R-051 ainda cobre os testes completos de segurança dos itens.

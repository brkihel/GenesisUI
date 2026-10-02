# Roteiro R-062 — personagem e itens no preview 3D

| Campo | Valor |
|---|---|
| Pacote | `GenesisMods-GenesisUI-1.1.1-preview.1.zip` |
| Tempo estimado | ~5 min |
| Pré-requisito | Mundo local no perfil Gale `test` |

## O que estamos testando

Se o personagem e os itens que ficaram invisíveis aparecem no inventário após a correção da composição do preview 3D.

## O que não estamos testando

- Movimentação, contagem ou ordenação de itens.
- Outras janelas e efeitos do HUD.

## Preparação

1. Instale o pacote no perfil Gale `test`, com BepInEx, Jötunn e GenesisUI. Use um mundo local; não use servidor.
2. Deixe `[Theme] Models3D = true`. Tenha um martelo e, se possível, uma pedra e outro item que antes aparecia no preview.

## Passos

| # | Faça | Esperado | Se for diferente, anote |
|---|---|---|---|
| 1 | Abra o inventário e aguarde dois segundos. | O personagem aparece entre os espaços de equipamento, vestido como o jogador, e gira suavemente. | Envie uma captura `R-062-01.png` da janela inteira. |
| 2 | Passe o mouse sobre o martelo e espere dois segundos. | O modelo 3D do martelo aparece na área de detalhes e gira, sem retângulo rosa nem borda rosa. | Envie `R-062-02.png`, incluindo a área de detalhes. |
| 3 | Passe o mouse sobre a pedra e sobre outros dois itens, incluindo algum que antes aparecia. | Cada modelo 3D disponível aparece, com o painel limpo ao trocar de item; itens sem modelo conservam seu ícone. | Anote os nomes dos itens que falharem e envie uma captura. |
| 4 | Feche e reabra o inventário. Repita o martelo. | Personagem e martelo continuam visíveis; a janela não mostra partes do inventário vanilla. | Anote qualquer mudança ou falha. |
| 5 | Ao final, pressione F8 e salve o relatório. | As linhas `[GenesisUI:Preview]` incluem `pixels RGB`, `RGB with alpha~0` e `keyed True` para personagem e martelo, sem falhas de módulo. | Envie o caminho do relatório, mesmo se todos os passos passaram. |

## Critério de aprovação

Os modelos dos passos 1–4 estão visíveis, sem cor rosa residual, e o relatório não contém falha do preview.

## O que me enviar

O relatório F8 **no fim da execução**, uma linha dizendo quais itens apareceram e as capturas dos passos que falharem.

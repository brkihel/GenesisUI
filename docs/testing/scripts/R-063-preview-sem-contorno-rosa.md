# Roteiro R-063 — preview sem contorno rosa

| Campo | Valor |
|---|---|
| Pacote | `GenesisMods-GenesisUI-1.1.1-preview.2.zip` |
| Tempo estimado | ~3 min |
| Pré-requisito | R-062: personagem e itens visíveis |

## O que estamos testando

Se o personagem e os itens continuam aparecendo com bordas suaves, sem o contorno rosa visto no pacote anterior.

## O que não estamos testando

- Transferência, ordenação ou contagem de itens.
- Outras janelas e efeitos do HUD.

## Preparação

1. Instale o pacote no perfil Gale `test`, com BepInEx e Jötunn, e abra um mundo local.
2. Mantenha `[Theme] Models3D = true`. Use os mesmos itens do teste anterior.

## Passos

| # | Faça | Esperado | Se for diferente, anote |
|---|---|---|---|
| 1 | Abra o inventário e observe cabeça, ombros e armas do personagem por dois segundos. | O personagem continua visível, com bordas suaves e sem linha rosa. | Envie uma captura `R-063-01.png` da janela inteira. |
| 2 | Passe o mouse sobre o martelo e acompanhe uma volta do modelo. | Ele permanece visível em qualquer ângulo, sem contorno rosa. | Envie `R-063-02.png` e descreva o ângulo da falha. |
| 3 | Observe o troféu de Eikthyr e o olho de anão-cinzento, se disponíveis, ou dois outros itens usados antes. | Pontas finas e efeitos continuam legíveis, sem rosa residual ou retângulo de fundo. | Anote o item e envie uma captura se houver alteração indesejada. |
| 4 | Mova o mouse pela janela; feche e reabra o inventário. | O preview continua visível e suas bordas ficam limpas durante o movimento. | Anote qualquer piscada, serrilhado forte ou halo. |
| 5 | No final, pressione F8 e salve o relatório. | As linhas de preview incluem `keyed True`, `AA 1`, `filter Point` e o tamanho do alvo, sem falhas de preview. | Envie o relatório mesmo se tudo passou. |

## O que me enviar

O relatório F8 **ao final**, se o rosa sumiu e se as bordas ficaram suaves, junto das capturas dos passos que falharem.

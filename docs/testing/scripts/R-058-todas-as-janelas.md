# Roteiro R-058 — Todas as janelas do jogo no GenesisUI

| Campo | Valor |
|---|---|
| Pacote | `GenesisMods-GenesisUI-0.8.0-preview.1.zip` |
| Pré-requisito | Perfil `test`, personagem com martelo, alguns materiais e moedas; um mundo com Haldor ajuda. |
| Não testado aqui | Menu inicial e configurações do jogo (continuam vanilla). |

Em qualquer passo: se algo cair, o F8 → relatório basta. Nada do visual vanilla deve aparecer por
baixo das nossas janelas.

| # | Faça | Esperado |
|---|---|---|
| 1 | Inventário → aba **Habilidades**. | Personagem (nome, dia, anel com o total de habilidades, poder do Guardião, atributos, botão PvP), habilidades por grupo com nível e barra, efeitos ativos no fim da lista, e Textos à direita (clicar num tópico mostra o texto). |
| 2 | Passe o cursor numa habilidade; clique no PvP (se o mundo permitir). | A descrição aparece embaixo; o PvP alterna (e só se o jogo deixar). |
| 3 | Aba **Conquistas**. | Conquistas com contador, troféus à direita; clicar mostra os detalhes. |
| 4 | Aba **Configurações**. | Categorias à esquerda; opções no meio; passe o cursor numa opção → explicação, padrão e "Restaurar". Mude uma escala do HUD e veja mudar; troque uma tecla (clique e aperte). |
| 5 | Equipe o martelo e abra o menu (botão direito). | Nosso menu: abas, categorias, busca, grade de peças; cursor numa peça mostra materiais e estação. Clique escolhe e fecha; botão do meio favorita. A busca aceita digitação sem mexer o personagem. |
| 6 | Com a peça escolhida, posicione. | Cartão "Posicionar" com a peça e os materiais (vermelho quando falta). |
| 7 | Fale com o Haldor. | Loja: itens com preço, detalhes, suas moedas, Comprar e Vender (mostra o que será vendido). |
| 8 | Divida uma pilha (Shift+clique), escolha um estilo na bancada, leia uma pedra rúnica, deixe o corvo falar, nomeie uma placa. | Cada um num cartão do GenesisUI, funcionando como no jogo. |
| 9 | Abra o mapa (M ou aba Mapa). | Moldura com abas em cima, título/dia, "sob o cursor", filtros, paleta de marcadores à direita, visível/zoom/centralizar embaixo. Duplo clique marca, botão direito remove. Q/E ou clique numa aba volta às janelas. |
| 10 | Esc no jogo. | Menu de pausa do GenesisUI com as opções do jogo; Sair pede confirmação no nosso cartão. |

Ao final, grave o relatório no F8 e envie-o com o `LogOutput.log`.

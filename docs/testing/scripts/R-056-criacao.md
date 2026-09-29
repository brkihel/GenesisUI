# Roteiro R-056 — Aba Criação (ConceptArt 12)

| Campo | Valor |
|---|---|
| Pacote | `GenesisMods-GenesisUI-0.7.0-preview.12.zip` |
| Pré-requisito | Perfil `test`, personagem com alguns materiais (madeira, pedra, resina, couro). |
| Não testado aqui | Painel Construção (próxima etapa) e o seletor de quantidade (− 1 +). |

| # | Faça | Esperado | Se for diferente, envie |
|---|---|---|---|
| 1 | Sem estação por perto, abra o inventário e vá para Criação (Q/E ou clique). | Painel Criação no meio (como no concept): Criar/Aprimorar, busca, categorias, lista de receitas de mão (tocha, martelo...), cartão de detalhes, materiais e Criar. Nada da criação vanilla aparece. | Print. |
| 2 | Clique em receitas diferentes. | O cartão mostra ícone, nome, tipo, descrição e tabela; materiais com "tenho / preciso" (verde ou vermelho). | Print. |
| 3 | Crie uma tocha. | O botão enche com o progresso e o item vai para o inventário; materiais descontados. | Quantidades antes/depois. |
| 4 | Digite na busca "mach" e troque de categoria (Armas, Ferramentas...). | A lista filtra na hora. Enquanto digita, Tab e E não fecham a janela nem trocam a aba. | Print. |
| 5 | Vá até uma bancada e abra. | Nome da estação e nível no topo; receitas da bancada; Reparar aparece (ativo se houver item danificado). Receitas sem material aparecem esmaecidas. | Print. |
| 6 | Aba Aprimorar com um item aprimorável. | Lista os itens seus com o próximo nível; o cartão mostra o aviso do jogo ("Aprimorar ... para nível N"); Criar vira Aprimorar. | Print. |
| 7 | Um item com estilos (ex.: escudo com variantes) na bancada: clique Estilo. | Abre o seletor de estilo do jogo por cima da janela; escolher funciona. | Print. |
| 8 | Role a roda do mouse sobre a lista longa. | A lista rola e a barrinha acompanha. | — |
| 9 | Troque para Inventário e volte; feche e abra. | Tudo volta; sem piscar a janela vanilla. | Relatório F8 se cair. |

Ao final, grave o relatório no F8 e envie-o se algo cair.

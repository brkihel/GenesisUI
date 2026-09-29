# Roteiro R-051 — Painel de equipamento e ajustes visuais

| Campo | Valor |
|---|---|
| Pacote | `GenesisMods-GenesisUI-0.7.0-preview.7.zip` |
| Pré-requisito | Faça backup do personagem de teste; use o perfil `test`. |

Antes de começar, anote ou capture os itens e quantidades do inventário. Use itens de teste.

| # | Faça | Esperado | Se for diferente, anote |
|---|---|---|
| 1 | Abra o inventário, espere 5 segundos, troque de aba e volte. | Ícones, quantidades e seleção ficam **à frente** da textura dos painéis; os slots permanecem alinhados e clicáveis. | Print aberto e depois de 5 segundos. |
| 2 | Gaste e recupere vigor algumas vezes. | A ponta da barra horizontal tem brilho quente que pulsa e pequenas faíscas em movimento. Não é só uma linha branca parada. O rastro é discreto. | Vídeo curto ou descrição. |
| 3 | Vista cabeça, peito, pernas, capa, cinto e trinket pelo inventário; feche e reabra a janela. | Cada peça vai para o espaço com seu nome no painel Equipamento. Armadura total acompanha o jogo; quantidades não mudam. | Print e itens usados. |
| 4 | Substitua uma peça vestida por outra do mesmo tipo, com o inventário quase cheio. Depois desequipe. | A nova peça ocupa o espaço de equipamento; a antiga fica no espaço de origem. Desequipar manda a peça para um espaço livre comum. | Posições antes/depois. |
| 5 | Arraste um tipo errado e uma pilha para um espaço de equipamento; depois arraste o tipo certo. | Tipos errados e pilhas são recusados sem mover itens. O tipo certo entra e é equipado pelo Valheim. | Posições e mensagens. |
| 6 | Encha os espaços comuns e tente desequipar uma peça. | A peça continua acessível no espaço de equipamento; nada cai nem desaparece. Libere um espaço e repita. | Quantidades antes/depois. |
| 7 | Com uma peça vestida, salve, saia e entre novamente. Depois injete falha em `win.inventory` pelo F8 e reative. | A peça permanece no espaço correto. Na falha, a grade vanilla expõe os itens; na reativação, o painel volta sem hover preso ou itens duplicados. | Print e quantidades. |
| 8 | Abra um baú e teste uma troca simples; feche o baú. | Itens do baú não entram diretamente num espaço de equipamento. Troca via espaço comum funciona como no jogo. | Posições antes/depois. |

Ao final, confira as quantidades, grave um relatório no F8 e envie o `.log`, o
`LogOutput.log` e os prints ou vídeo dos passos que divergirem.

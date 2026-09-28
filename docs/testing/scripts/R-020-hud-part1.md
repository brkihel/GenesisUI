# Roteiro R-020 — HUD parte 1: arte, comida, barra de itens e efeitos

| Campo | Valor |
|---|---|
| Pacote | `GenesisMods-GenesisUI-0.3.0-preview.1.zip` |
| Tempo estimado | ~20 min |
| Pré-requisito | R-010 (comportamento das barras aprovado na 0.2.0) |

## O que estamos testando

1. Que a **arte agora aparece**: molduras douradas, medalhão, placa da barra de itens,
   quadros de efeitos. Na 0.2.0 apareceram só retângulos lisos.
2. Três módulos novos, só de exibição: **comida**, **barra de itens** e **efeitos com o
   poder do guardião**.

## O que NÃO estamos testando

- Minimapa, bússola, texto de interação, barra de inimigo: ainda são os do jogo.
- A sobreposição com o minimapa do jogo: os efeitos ficam abaixo dele por enquanto.
- De novo o comportamento das barras de vida/vigor/eitr (já aprovado), a não ser que algo
  pareça diferente.

## Preparação

1. Substitua o pacote no perfil de teste. Em `BepInEx/plugins/GenesisMods-GenesisUI/art/`
   agora há **7 imagens** + `sprites.json`.
2. Mundo `GenesisUI-Teste`. Tenha à mão: 2 ou 3 comidas diferentes, uma ferramenta ou arma
   com durabilidade (machado, clava), algo empilhável (madeira, pedras). Se o personagem
   tiver um poder do guardião (Eikthyr), melhor; se não tiver, pule o passo 9.

## Passos

| # | Faça | Esperado | Se for diferente, anote |
|---|---|---|---|
| 1 | Entre no mundo e olhe a tela inteira. | As **barras de vida e vigor com moldura dourada em arco**, losango no topo e o **medalhão** embaixo. **Barra de itens** no centro de baixo: placa escura com linha dourada e um **nó em cada ponta**, 8 espaços numerados. **Três espaços de comida** à direita das barras. Nada do HUD original nesses lugares. | Print `R-020-01.png` da tela inteira. |
| 2 | Abra o F8. | Em *Módulos*: `hud.vitals`, `hud.food`, `hud.hotbar`, `hud.status`, todos `Active`. Na linha do log/relatório de tema: `sprites: 7`. | Print do painel `R-020-02.png`. |
| 3 | Coloque na barra de itens (1-8) a ferramenta, uma pilha de madeira e uma arma. | Cada item aparece no espaço certo, com o **número da tecla** no canto de cima e a **quantidade** embaixo à direita (só em pilhas maiores que 1). | Anote o espaço e o que viu. |
| 4 | Equipe um item pela tecla (ex.: **1**). | O espaço ganha a **moldura dourada brilhante**; ao desequipar, ela some. | Print `R-020-04.png`. |
| 5 | Use a ferramenta até gastar um pouco (corte uma árvore). | Aparece uma **barrinha verde** embaixo do espaço, que diminui com o uso. | Anote. |
| 6 | *(Se tiver controle)* Use LB/RB para mudar a seleção da barra. | O espaço selecionado ganha a moldura dourada; o uso pelo botão continua funcionando. | Anote. |
| 7 | Coma 2 ou 3 comidas. | Cada comida aparece num dos espaços ao lado das barras, com o **tempo restante** embaixo ("30m") e uma **barrinha** do que resta. Os valores batem com o que o jogo mostraria. | Print `R-020-07.png`. |
| 8 | Espere uma comida chegar perto do fim (ou coma algo de duração curta). | Quando ela pode ser comida de novo, o ícone **pulsa**. No último minuto o tempo vira segundos ("45s") e **pisca**. | Anote. |
| 9 | Ative o poder do guardião (**F** / tecla do poder) e espere ele acabar. | No canto de cima à direita, **abaixo do minimapa**, um quadro com o ícone do poder, o **nome** e o **tempo** (m:ss). Durante o efeito, aparece também o efeito do poder; depois, a recarga conta até 0:00 e a moldura fica **dourada** quando o poder está pronto. | Print `R-020-09.png`. |
| 10 | Fique **descansado** (perto da fogueira, sentado ou dormindo) e **molhado** (chuva ou água). | Quadros de efeito aparecem lado a lado com nome e tempo. Efeitos que piscam no jogo (ex.: frio, molhado) também piscam aqui. | Print `R-020-10.png`. |
| 11 | No F8, em cada módulo novo, clique **Injetar falha** e depois **Reativar**. | Cada falha devolve **só aquele pedaço** ao visual original (comida, barra de itens ou efeitos do jogo); os outros continuam. Reativar traz o nosso de volta. | Anote qual não voltou. |
| 12 | **Mostrar vanilla** e **Esconder vanilla** no F8. | O original aparece por baixo de todos os nossos pedaços e some de novo. | Print com o vanilla à mostra `R-020-12.png`. |
| 13 | Morra (ou use o console para isso) e renasça. | Durante a morte, barra de itens, comida e efeitos somem; ao renascer, voltam sem duplicar. | Anote. |
| 14 | Saia para o menu e entre de novo. | Tudo aparece uma vez só, no lugar. | Anote. |

## Critério de aprovação

- Passos 1 a 14 com o "Esperado" confirmado (6 e 9 dependem de ter controle e poder), e
- nenhuma linha `[Error]` de `GenesisUI` no log, exceto as falhas injetadas de propósito.

## O que me enviar

1. O relatório do F8 (**no fim** do roteiro desta vez).
2. `BepInEx/LogOutput.log` e o log de `BepInEx/GenesisUI/logs/`.
3. Os prints.
4. **A sua opinião sobre o visual**, agora que a arte aparece: tamanhos, posições, cores,
   fontes, o que difere da concept-art. Posições ficam no config em `[Vitals]`, `[Food]`,
   `[Hotbar]` e `[Status]`.

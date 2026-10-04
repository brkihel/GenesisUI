# R-070 — Mochila, escalas e fluxo das gemas

| Campo | Valor |
| --- | --- |
| Pacote | `GenesisMods-GenesisUI-1.2.0-preview.2.zip` |
| Perfil | Gale **GenesisHeimLocal** |
| Tempo estimado | 10–15 minutos, um jogador |
| Pré-requisito | Retorno do Preview.1 registrado em R-069 |

## O que estamos testando

O encaixe da mochila, os dois controles de escala e a edição de gemas sobre os detalhes,
mantendo a mesa Gemcutter aberta e usando as regras nativas dos mods.

## O que NÃO estamos testando

- Certificação de todo o modpack, desempenho, servidor ou multiplayer.
- Adventure Backpacks, ausente deste perfil, e novas regras de criação/remoção de gemas.
- Carteira, chaves, barras e tooltips em profundidade: já têm o roteiro R-069.

## Preparação

1. Feche o jogo e atualize somente o GenesisUI no perfil **GenesisHeimLocal**, usando o ZIP
   acima. Continue no seu cliente, em mundo local/personagem de teste com cópia de segurança.
2. Mantenha Backpacks **1.3.10**, Jewelcrafting **2.0.10** e as configurações atuais da mesa.
   Tenha uma mochila, equipamento com sockets e gemas que possam ser usadas nele.
3. Comece com **Geral → Escala geral da UI = 1** e **Janelas → Escala das janelas = 1**
   nas configurações do GenesisUI. No arquivo são `[General] Scale` e `[Windows] Scale`.
   As opções anteriores de largura/altura continuam disponíveis.

## Passos

1. **Faça:** desequipe/equipe a mochila pelo clique normal. Depois retire-a e arraste-a
   para o slot de mochila. Feche/reabra o inventário e entre novamente no mundo.
   **Esperado:** a mochila equipada ocupa seu slot, com o tempo de equipamento normal.
   Nenhum item some, duplica ou troca de quantidade; os itens da mochila permanecem intactos.
2. **Faça:** passe o mouse na mochila e pressione **E**. Movimente um item para dentro e
   de volta. Feche a mochila pelo botão e repita.
   **Esperado:** E abre a mochila sem mudar de aba; seus slots ficam abaixo do inventário,
   com cliques/arrastes e fechamento normais.
3. **Faça:** com a mochila aberta, ajuste **Escala das janelas** para **1,2** e **1,3**.
   Experimente também reduzir para **0,9**; depois volte a 1. Abra as outras janelas.
   **Esperado:** painéis, molduras, slots, modelo e barras de abas crescem/diminuem juntos,
   sem distorção nem desalinhamento. O HUD conserva seu tamanho. O aumento para ao chegar
   ao limite da tela; isso é esperado e evita cortar os botões. Anote o valor mais confortável.
4. **Faça:** ajuste **Escala geral da UI** para **0,9** e **1,1**. Feche as janelas,
   confira o HUD e uma placa de criatura; reabra as janelas. Restaure 1 nos dois controles.
   **Esperado:** HUD e janelas mudam de escala, mantendo as proporções e os pontos de ancoragem.
   A escala das janelas continua independente e combina com a geral. Os valores persistem
   após fechar/reabrir o jogo.
5. **Faça:** interaja com a Gemcutter's Table, abra **Sockets**, escolha um equipamento e
   clique em **Gemas**. Use a roda do mouse na lista de itens se a gema estiver mais abaixo.
   **Esperado:** aparece o painel **Gemas do equipamento** no espaço dos detalhes. Os detalhes
   antigos não aparecem através da transparência. A mesa, a aba e a seleção continuam abertas;
   a lista de receitas fica temporariamente sem clique enquanto você edita as gemas.
6. **Faça:** coloque uma gema compatível no socket pelo clique/arraste normal. Remova uma
   somente se as regras atuais do Jewelcrafting permitirem. Experimente um item incompatível
   e a divisão de uma pilha pelo atalho normal.
   **Esperado:** aceitação, rejeição, custos/chance de quebra e salvamento seguem o mod.
   O cursor mostra o item arrastado; tooltips e a divisão de pilhas funcionam. A interface
   não fecha nem troca para o inventário. Não repita remoções de risco sem necessidade.
7. **Faça:** clique em **Fechar e voltar aos detalhes**. Abra as gemas desse equipamento
   novamente duas vezes; depois selecione outro equipamento e repita. Feche/reabra a mesa.
   **Esperado:** fechar retorna aos detalhes do mesmo item sem reabrir a mesa ou escolher
   a aba novamente. Sockets atualizados e persistentes, sem slots/cursores duplicados ou
   tooltip preso. Ao sair da aba, a edição fecha pelo comando nativo e salva normalmente.
8. **Faça:** no diagnóstico do Preview, injete uma falha em `win.crafting` durante a edição;
   use a recuperação do módulo e reabra a mesa. Execute este passo por último.
   **Esperado:** a janela fecha com a mensagem de falha; a recuperação não deixa cursor,
   camada de gemas ou detalhes escondidos. A mochila e os demais módulos continuam disponíveis.

## Critério de aprovação

Passos acima confirmados, sem itens perdidos/duplicados, sem erros inesperados de GenesisUI
e com o fluxo de gemas confortável. A falha injetada do último passo é intencional.

## O que me enviar

Ao terminar, pressione **F8** e use **Gerar relatório** no painel; envie o arquivo indicado,
`BepInEx/GenesisUI/logs/` e `BepInEx/LogOutput.log` da sessão. Informe quais passos passaram,
o valor de escala preferido e, se houver problema, um print `R-070-<passo>.png`.

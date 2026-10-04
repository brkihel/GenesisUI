# R-069 — Integrações do inventário e barras

**Versão:** 1.2.0-preview.1. **Perfil:** Gale `GenesisHeimLocal`.
Um jogador, no seu cliente e em um mundo/personagem de teste com cópia de segurança.
Pacote: `dist/GenesisMods-GenesisUI-1.2.0-preview.1.zip`.

## Preparação

Instale o Preview no perfil informado. Tenha uma mochila Backpacks, anel/colar Jewelcrafting,
Hip Lantern, mesa Gemcutter, itens/gemas para testar, moedas e chaves vanilla.
Versões locais conferidas: Backpacks 1.3.10, Jewelcrafting 2.0.10, HipLantern 1.1.12.
Adventure Backpacks 2.0.3 é opcional e não está no perfil atual; marque seu teste como não
executado se continuar ausente. Mantenha as configurações nativas: sockets na mesa e slots
próprios de anel/colar/lanterna; custos e chance de quebra seguem o Jewelcrafting.

A carteira usa o limite Int32 do jogo (2.147.483.647), com peso e gastos normais. Pilhas antigas
separadas podem ser juntadas pelo arrastar nativo. **Antes de desinstalar o GenesisUI e outros
provedores de pilha ampliada, divida as moedas ao limite vanilla:** o carregamento vanilla
corta pilhas maiores quando não há um provedor. Desligar apenas a interface mantém a proteção
de carregamento enquanto o plugin continua instalado.

## Passos

1. Abra o inventário e equipe mochila, lanterna, anel e colar pelos cliques normais.
   **Esperado:** mochila/lanterna em ordem com os outros slots, anel/colar abaixo do personagem,
   carteira e duas chaves pequenas acima. Modelo inteiro, rótulos legíveis e alinhados.
   O tempo de equipar e a borda de progresso continuam nativos; sem item sumir ou duplicar.
2. Passe o mouse sobre moeda, mochila, joia e item comum.
   **Esperado:** tooltip escuro com borda dourada muito discreta, sem tapar desnecessariamente
   o inventário; textos/gemas/instruções dos mods continuam aparecendo. Detalhes completos
   também continuam disponíveis no painel. Retire o mouse e confira que o tooltip some.
3. Com o mouse sobre a mochila, pressione **E** (ou a sua tecla de Usar).
   **Esperado:** abre a mochila e mantém a aba Inventário. Conteúdo em painel abaixo, sem
   cobrir equipamento/detalhes; painel e barra de abas usam a mesma escala. E longe de um
   item com ação continua mudando de aba. Q continua funcionando.
4. Arraste itens para a mochila e de volta, divida uma pilha e role seu conteúdo quando houver
   mais linhas. Confira pegar/empilhar quando o mod disponibilizar os botões e use **Fechar**.
   **Esperado:** quantidades e restrições do mod preservadas, todos os slots acessíveis pela
   rolagem; fechar remove apenas o conteúdo aberto. Feche/reabra o inventário para conferir.
5. Interaja com a Gemcutter's Table, clique **Sockets** e selecione um item.
   **Esperado:** lista dos itens aceitos pelo Jewelcrafting, sem entrar na lista de upgrades
   vanilla. Botão/custos/aviso de sucesso ou quebra refletem o mod, inclusive com botão ativo.
   Use apenas um item de teste que possa perder pela regra configurada. Execute uma adição.
6. Selecione um item com sockets e clique **Inserir gemas**. Aloque uma gema pelo arrastar.
   **Esperado:** abre o inventário com os slots de gemas; inserção/remoção, incompatibilidades
   e possíveis custos/quebras seguem o mod. Feche o painel, volte à Criação e à lapidação.
   Sem fechar o inventário por engano ou misturar candidatos de sockets com upgrades vanilla.
7. Junte mais de 500 moedas na carteira, anote o total e coloque as duas chaves nos slots.
   Arraste moeda/chave de volta ao inventário; tente material comum na carteira e moedas nas
   chaves; tente trocar uma chave por item em slot de consumo/ação.
   **Esperado:** moedas juntam pela operação nativa, duas chaves aceitas (CryptKey/DvergrKey),
   combinações inválidas recusadas sem troca de quantidade/identidade. Novas moedas juntam
   na pilha existente se houver capacidade/peso nativo. Compre algo se houver comerciante,
   conferindo o gasto; informe se esta compra não foi testada.
8. Feche/reabra a mochila, saia ao menu e volte ao mesmo mundo/personagem. Reconfira moedas,
   chaves, joias e conteúdo da mochila. Depois desligue/religue Pockets e os módulos de
   integração/inventário pelas configurações; faça outra saída/entrada com a UI desativada.
   **Esperado:** mesmas quantidades/conteúdo; posições migradas com segurança e fallback
   acessível. Nenhuma pilha de moedas cortada a 500. Se o inventário comum estiver cheio,
   a mudança de layout pode ser recusada com aviso até haver espaço. Reative os módulos.
9. Pegue o efeito Descansado e confira HP/vigor/eitr com e sem o inventário aberto; confira
   a escala de interface habitual e uma escala menor/maior, mapa grande e menu Esc.
   **Esperado:** nenhum retângulo/brilho envolvendo as barras e comida. Luz segue somente
   as molduras das barras; líquidos/níveis e leitura continuam corretos. Sem sobreposição
   entre mochila, abas, dicas, personagem, tooltips e outras janelas.
10. **Opcional:** em um perfil de teste com Adventure Backpacks 2.0.3, equipe a mochila e
    pressione Usar sobre ela; transfira itens e recarregue o personagem como nos passos 3–8.
    **Esperado:** slot e painel abaixo funcionam; abertura exige mochila equipada e regras
    nativas do mod. Restrições de capa/equipamento continuam as do Adventure Backpacks.
11. Ao terminar, pressione **F8**, gere e envie o relatório. Diga quais passos passaram, os
    não executados e envie captura de defeitos/alinhamento, se houver.

## Fora deste teste

Sem aprovação visual prévia deste Preview, benchmark, servidor/produção, segundo jogador,
certificação de outras versões ou do modpack completo. Não instala DLLs de referência dos
mods no pacote; elas foram usadas apenas pelos testes de contrato locais.

# R-065 — Base de estabilidade antes dos adaptadores

Pacote: **1.1.2-preview.1**, branch `fix/stability-1.1.2`.
Referência visual: 1.1.1-preview.2 aprovado. O shader é o mesmo; o código dos previews mudou.

## Preparação

Instale o ZIP no perfil **test**, como de costume. Use personagem/mundo locais descartáveis para
mudar capacidade/slots; registre os itens e suas quantidades antes. Não use personagem do servidor
nessas etapas. Uma pessoa executa tudo. Anote resolução, escala da interface e mods presentes.

Este roteiro verifica a base corrigida. Slots próprios de mochila/joias/lanterna, novos provedores
de sockets/magia e certificação de todos os recursos do servidor ficam para os próximos adaptadores.
Não alterar o FullPlaythrough original. Etapas sem conteúdo/hardware disponível podem ser marcadas
como **não executadas**, com o motivo.

## Teste

1. **Abertura e conteúdo:** abra inventário, baú, criação, habilidades, conquistas, configurações,
   menu Esc, construção e mapa. Faça isso nas escalas de interface que você costuma usar.
   **Esperado:** um painel funcional por vez, sem desenho vanilla vazando ou área invisível
   bloqueando cliques. O ouro e os modelos preservam o aspecto aprovado.

2. **Personagem e itens 3D:** passe pelo martelo, armas e itens que antes sumiam. Troque duas peças
   de equipamento mantendo a mesma quantidade de peças, guarde/saque a arma e reabra o inventário.
   Compare a luz do mundo antes/depois. Se algum item não tiver modelo, observe o ícone.
   **Esperado:** personagem/equipamento atualizam, sem outline rosa, efeitos/sons duplicados ou luz
   vazando do palco. Item sem modelo mantém ícone utilizável. Envie captura se aparecer diferença.

3. **Informações do item:** selecione item com tooltip longo ou informações de mod, role o painel
   de detalhes e compare com o tooltip original. Repare/melhore um item e volte aos seus detalhes.
   **Esperado:** texto original completo, incluindo dados acrescentados ao tooltip por mods;
   durabilidade/qualidade atualizadas no mesmo item. Componentes interativos exclusivos de mods
   ainda precisam dos seus adaptadores: registre qualquer informação/interação que faltar.

4. **Entradas originais:** arraste, divida pilha, transfira entre inventário/baú, use item e teste
   Shift/Ctrl e combinações configuradas. Solte o modificador antes/depois da tecla principal.
   Escreva no chat, busca de construção/receitas e campos de configuração; teste slots desligados.
   **Esperado:** as regras e ações originais continuam funcionando; escrever não usa itens;
   atalhos desligados não engolem ações e mudar a combinação limpa a anterior.

5. **Controle/toque, se disponíveis:** percorra inventário e baú grandes até precisar rolar,
   incluindo slots especiais. Selecione, divida, transfira e use com o controle; teste arrastar/soltar
   no dispositivo de toque, se você realmente usa um.
   **Esperado:** foco/scroll mostram a célula que a ação original alcança, sem transferência dupla.
   Mouse não substitui esta etapa; sem hardware, marque-a como não executada.

6. **Mudança de capacidade e organização:** no personagem local descartável, coloque itens nas
   linhas inferiores, aumente/reduza linhas e slots nas configurações; teste inventário cheio e o
   botão Organizar. Salve, saia e entre de novo; compare referências visuais, pilhas e quantidades.
   **Esperado:** nenhum item some, duplica ou cai no mundo por mudança de capacidade. Uma redução
   sem espaço pode ser recusada com diagnóstico; os itens continuam disponíveis. Não editar o save.

7. **Recuperação:** no F8, role a lista para alcançar `win.inventory` e injete uma falha uma vez.
   Feche o aviso/diagnóstico e reabra o inventário. Repita uma vez em `win.shell` e uma em `hud.food`.
   **Esperado:** aviso explica a falha injetada, limpeza ocorre antes da reconstrução e não há
   painel duplicado, arrasto preso ou bloqueio permanente do teclado/ponteiro.

8. **Ordem de desligamento:** desligue as janelas de inventário/criação nas duas ordens e reative
   nas duas ordens. Desligue a shell com uma janela aberta e reative-a; repita abrindo/fechando baú.
   **Esperado:** dependentes deixam de esconder vanilla quando a shell não está disponível;
   painéis, dicas de teclas e arrasto voltam corretamente ao último dono liberar a área.

9. **Efeitos e materiais:** desligue/religue Comida com poções/efeitos ativos, usando mais de cinco
   quando houver conteúdo suficiente. Compare requisitos de receitas, especialmente recursos que
   compartilham ícone e quantidades coloridas pelo jogo/mod.
   **Esperado:** efeitos que Comida não mostra continuam na lista de status; requisitos identificam
   o item correto e preservam a quantidade/cor original, sem adivinhar por ícone ou texto formatado.

10. **Mapa e vitais:** marque um ponto, altere nome/ícone pelos controles originais, desligue/religue
    marcadores GenesisUI e confira a aparência. Observe o indicador climático junto dos vitais.
    **Esperado:** desligar restaura a aparência atual, incluindo alterações posteriores do dono
    original; o indicador segue o grupo correto e some quando o módulo necessário não está ativo.

11. **Cenas:** saia para seleção de personagem e entre no mundo local **três vezes**; depois abra
    inventário, baú, mapa e menus e use um atalho.
    **Esperado:** nenhum som, janela, efeito, atalho ou preview duplicado; itens intactos.

12. **Convivência, opcional:** em uma cópia separada do FullPlaythrough, mantendo SeneaL UI ativo,
    instale este preview e abra HUD/janelas/construção/mapa.
    **Esperado:** regiões conhecidas do SeneaL aparecem `Blocked` no F8 e permanecem com seu dono.
    Isto verifica a proteção de conflito; não certifica os futuros recursos/adaptadores.

13. **Relatórios no fim:** espere cerca de cinco segundos com inventário fechado, abra F8 e role
    até **Gerar relatório**. Feche F8, abra inventário com personagem/item visíveis, espere cinco
    segundos e gere outro relatório. Se quiser, clique novamente para conferir nomes diferentes.
    **Esperado:** arquivos distintos; origem/hash do shader, métricas de quadros e dos palcos presentes.
    As métricas não são um benchmark isolado da GenesisUI e não medem tempo de GPU.

## Enviar

Envie os relatórios F8 gerados no fim, dizendo quais passos passaram, falharam ou não foram executados.
Inclua captura de qualquer diferença visual, quantidade/itens antes/depois e lista/versões dos mods
do perfil usado. A falha injetada do passo 7 é esperada; anote comportamentos que continuarem errados.

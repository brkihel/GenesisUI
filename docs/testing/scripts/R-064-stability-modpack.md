# R-064 — Estabilidade e compatibilidade do modpack

**Estado: roteiro planejado. Não há um novo pacote para executá-lo nesta revisão.**
Base visual aprovada: `1.1.1-preview.2`, commit `2a1854a`.
Os lotes de correção/integração terão seus próprios previews e indicarão quais etapas estão prontas.
Este roteiro acompanha [a revisão](../../review/2026-10-02/README.md).

## Objetivo e preparação

Verificar recuperação, integridade dos itens e acesso aos recursos habilitados do FullPlaythrough.
Usar uma cópia do perfil de cliente, personagem e mundo locais descartáveis. Não usar personagem
do servidor para testar morte, redução de inventário ou mudanças de slots. Uma pessoa executa tudo.
Não alterar o perfil FullPlaythrough original durante o teste.

Antes de cada lote, anotar pacote/SHA do GenesisUI, versões dos mods, recursos habilitados,
resolução e escala da interface. Comparar com a mesma cópia de perfil sem GenesisUI quando for
necessário confirmar o comportamento original. As regras de configuração bloqueadas pelo servidor
serão verificadas apenas em uma etapa autorizada e com ambiente indicado; não testar no servidor
de produção nem tentar contornar bloqueios.

## Etapas por lote

1. **Inventário básico:** abrir, arrastar, dividir, mover para baú e usar itens; abrir crafting,
   reparar e melhorar um item disponível. Repetir com inventário comum cheio.
   **Esperado:** ações seguem as regras originais, nenhum item some/duplica e o painel selecionado
   atualiza durabilidade/qualidade sem precisar trocar o hover.
2. **Recuperação:** somente quando o preview do lote indicar a ferramenta de injeção, provocar
   uma falha em um módulo pela tela de diagnóstico. Reabrir a janela e repetir a ação original.
   Alternar os módulos envolvidos nas duas ordens indicadas pelo lote.
   **Esperado:** aviso claro, recuperação limitada, um único painel funcional, ponteiro/atalhos
   liberados corretamente. Sem objetos/leases acumulados ou painel invisível bloqueando cliques.
3. **Ciclo de cenas:** fechar inventário, sair para seleção de personagem e entrar novamente no
   mundo local. Reabrir inventário, mapa, menus e baú. Repetir o número indicado pelo lote.
   **Esperado:** nenhuma janela, efeito, som, atalho ou contador aparece duplicado.
4. **Entradas:** testar atalhos normais e combinações, soltar modificador antes/depois da tecla,
   escrever no chat, busca de construção e campos de configuração. Testar slots desabilitados.
   Se usar controle, navegar/rolar/selecionar/dividir/mover/usar com o controle.
   **Esperado:** escrever não usa itens; atalhos desabilitados não engolem ações; foco e seleção
   visíveis correspondem ao alvo da ação.
5. **Mochila:** equipar, abrir/fechar, transferir nos dois sentidos e testar mochila cheia.
   Reentrar no mundo com conteúdo guardado. Só testar upgrade/redimensionamento se habilitado.
   **Esperado:** capacidade, peso e regras originais; conteúdo e item da mochila permanecem íntegros.
6. **Joalheria:** comparar tooltip de item com/sem gemas, equipar anel e amuleto, mudar efeito de
   um mesmo item e abrir a interface original de gemas/mesa.
   **Esperado:** slots independentes, informações completas e acesso à interação original;
   nenhum setter/transferência nova contorna as regras do Jewelcrafting.
7. **Lanterna e preview:** equipar lanterna junto de mochila/joias/cinto; trocar equipamentos
   mantendo quantidade semelhante de peças. Abrir/fechar preview e usar tecla original da lanterna.
   **Esperado:** modelos aparecem, sem borda rosa, som/efeito do mundo duplicado ou luz vazando do
   palco. Em falha de render/provider, aparece fallback utilizável com diagnóstico.
8. **Magia/efeitos:** usar magias/consumíveis configurados, comparar recursos e cooldowns com
   a interface original. Desligar o módulo de comida e testar várias poções/efeitos simultâneos.
   **Esperado:** todas as informações relevantes continuam acessíveis, sem efeito oculto pelo limite
   de tiles ou pelo desligamento de outro módulo.
9. **Persistência combinada:** no personagem local descartável, com mochila/joias/equipamentos,
   morrer, recuperar os itens sozinho, salvar e reentrar. Antes/depois, registrar itens e quantidades.
   Mudanças de layout só nas condições previstas no lote; nunca editar posições salvas manualmente.
   **Esperado:** regras de ZenPlayer/Backpacks/ServerCharacters preservadas quando aplicáveis;
   identidades/conteúdos/quantidades intactos, sem item jogado por posição inválida.
10. **Mapa/HUD/progressão:** verificar indicadores de níveis, estação, horário, Sagas/Suite,
    habilidades extras e conquistas realmente habilitados. Testar modo sem mapa se disponível e
    desligar/reativar os módulos de marcadores.
    **Esperado:** cada recurso tem um lugar visível, textos legíveis e fallback; marcadores voltam
    à aparência original ao desligar. Habilidades/conquistas mantêm a informação original.
11. **Listas e valores grandes:** abrir baú grande, receitas que compartilham ícones, listas longas
    de construção e stacks altos. Abrir ferramentas de configuração e diálogos.
    **Esperado:** contagens identificam o recurso correto; scroll, foco, conteúdo e ações continuam
    acessíveis, sem conflito de donos de janela.
12. **Desempenho:** no mesmo PC e cenário, comparar HUD parado, inventário com previews e listas
    longas antes/depois. Guardar a captura de métricas pedida pelo lote, separando aquecimento e uso
    contínuo. Não concluir ganho/perda só pelo FPS de uma captura isolada.
    **Esperado:** limites acordados no lote, sem crescimento contínuo de recursos ou travadas novas.

## O que enviar

Ao **final** do lote, pressionar **F8** e enviar o relatório. Informar número de preview, etapas
executadas/puladas, comportamento esperado/observado e captura ou vídeo dos problemas visuais.
Para item/persistência, registrar antes/depois sem expor dados pessoais. Etapas de recursos ausentes
ou desabilitados devem ser marcadas como **não aplicáveis**, não aprovadas.

Nenhuma etapa deste documento foi executada em jogo durante a revisão de código. A certificação
final exige resultados registrados para os recursos e versões efetivamente usados no servidor.

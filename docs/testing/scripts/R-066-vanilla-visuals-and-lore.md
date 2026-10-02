# R-066 — Personagem, mapa, borda de Produzir, Esc e pedras de lore

**Versão:** 1.1.2-preview.2. **Perfil:** Gale `test`, primeiro com vanilla.
Um jogador; nenhum teste no servidor. Este roteiro verifica os problemas relatados no R-065.
Não certifica o FullPlaythrough nem os futuros adaptadores de recursos.

## Passos

1. Abra o inventário com capa equipada. Observe frente/costas enquanto o personagem gira
   suavemente; troque capa, capacete, peitoral, pernas e arma algumas vezes.
   **Esperado:** a capa acompanha a pose, sem ponta presa longe do corpo. O tamanho e a câmera
   permanecem constantes entre trocas e ao fechar/reabrir. A pose é uma fotografia 3D estática:
   a capa não simula vento nesse painel; o conjunto mantém o movimento suave de apresentação.
2. Veja os detalhes do martelo e de dois outros itens que já apareciam.
   **Esperado:** modelos, textos, rolagem e transparência continuam corretos.
3. Produza um item numa bancada e, se possível, aprimore outro.
   **Esperado:** brilho dourado discreto só na borda enquanto trabalha, sem preencher o botão
   ou encobrir as letras; termina normalmente. Com efeitos desativados, apenas um traço fino
   de progresso na base. Cancelar não deixa o brilho preso.
4. Abra o mapa com M e pela aba Mapa. Arraste, aproxime/afaste e crie/nomeie/remova um pino.
   **Esperado:** cores legíveis, sem camada escura cobrindo a imagem; moldura e controles visíveis.
   Confira também o minimapa depois de fechar. Repita na escala de interface que costuma usar.
5. Aperte Esc para abrir/fechar o menu pelo menos cinco vezes. Abra uma confirmação de sair
   ou desconectar e cancele; abra Configurações e volte.
   **Esperado:** nenhum flash do menu vanilla central, nenhum texto ou confirmação vanilla
   aparecendo atrás, nem uma camada residual ao retornar ao jogo.
6. Leia uma pedra de lore e uma pedra de sacrifício no templo inicial. Espere terminar a revelação,
   feche com E ou Esc e leia de novo. Afaste-se durante outra leitura.
   **Esperado:** um único texto sem janela/moldura, primeiro em runas e depois legível em português,
   com revelação mais lenta que os menus. Sem texto vermelho por baixo, cartão de interação por cima
   ou janela "TOPIC / KRAW KRAW KRAW". Quebras de linha e acentos permanecem corretos. O fechamento
   continua vanilla: E/Esc ou afastamento; não há flash vermelho na entrada nem na saída.
7. Se Hugin/Munin estiver disponível, converse uma vez. Teste ainda um baú/porta e um portal/placa.
   **Esperado:** só o cartão real do corvo; interação comum e digitação continuam funcionando.
8. Se possível pelo gerenciador de configuração, desligue e religue os módulos Menus/Diálogos
   e o mod inteiro enquanto essas telas estão fechadas, depois abra-as de novo.
   **Esperado:** quando desligado, vanilla volta corretamente; ao religar, nossas telas retornam
   sem duplicatas. Não precisa instalar outro mod só para cumprir este passo; informe se pulou.
9. **No final**, gere o relatório com F8. Envie o caminho, quais passos passaram/falharam/pulou e
   um print ou vídeo curto de qualquer flash/capa/texto/mapa incorreto.

Para o mapa, informe se o escurecimento é no mapa grande, no minimapa ou nos dois. Os testes
automáticos/GPU são evidência técnica; a aprovação visual é este retorno no cliente.

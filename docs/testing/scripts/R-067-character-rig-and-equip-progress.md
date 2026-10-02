# R-067 — Personagem 3D e brilho ao equipar

**Versão:** 1.1.2-preview.3. **Perfil:** Gale `test`, primeiro com vanilla.
Um jogador, só no cliente. Não certifica o modpack FullPlaythrough.

## Passos

1. Abra o inventário com capa. Aguarde o giro suave e confira corpo, cabeça, pernas e capa.
   **Esperado:** corpo inteiro no painel, tamanho normal e capa junto ao corpo. É uma pose
   estática do personagem real; não há simulação de vento no preview.
2. Troque capacete, peitoral, pernas e capa algumas vezes. Feche/reabra entre duas trocas.
   **Esperado:** equipamento atualizado, mesma escala/distância da câmera, sem ponta da capa
   presa longe do corpo, modelo gigantesco ou personagem afastando para sempre.
3. Clique para equipar uma peça com tempo de equipamento perceptível.
   **Esperado:** segmento dourado gira pela borda da célula durante a ação, sem preenchê-la;
   nenhuma barra amarela vanilla por trás. Só após equipar de fato, a peça ocupa seu slot de
   equipamento. Armas seguem os slots/regras atuais, sem criar slot novo.
4. Durante outra ação, clique novamente para cancelar. Depois coloque duas peças na fila.
   **Esperado:** cancelar remove o brilho sem mover a peça. A fila respeita o jogo: o brilho
   acompanha a peça que está sendo equipada agora e depois passa à próxima.
5. Durante uma ação, role o inventário até esconder a peça; feche a janela e reabra.
   **Esperado:** peça fora de vista tem texto/percentual discreto abaixo dos slots especiais;
   janela fechada devolve o progresso ao HUD nativo. Reabrir mostra o estado real, sem brilho
   preso numa célula vazia. Desequipar também acompanha a ação real.
6. Desative os efeitos visuais nas configurações GenesisUI e equipe outra peça.
   **Esperado:** borda estática e texto/percentual enquanto a ação ocorre, sem barra amarela
   atrás do inventário. Reative os efeitos depois.
7. Confira um item 3D nos detalhes, mapa grande e abertura do menu Esc. Experimente também
   a escala de interface que você costuma usar.
   **Esperado:** continuam corretos; personagem permanece enquadrado.
8. Ao terminar, pressione **F8**, gere o relatório e envie o caminho/arquivo, dizendo se os
   passos 1–6 passaram. Se surgir defeito e for possível, envie uma captura com o inventário aberto.

## Fora deste teste

Sem mudanças de quantidade/identidade de itens, tempo de equipamento, física do personagem
real ou operações do servidor. Não exige segundo jogador. Release aguarda a aprovação visual.

# Roteiro R-050 — Inventário e HUD após o R-049

| Campo | Valor |
|---|---|
| Pacote | `GenesisMods-GenesisUI-0.7.0-preview.6.zip` |
| Pré-requisito | Backup do personagem de teste; perfil `test`. |

1. Abra o inventário e espere alguns segundos. Os espaços devem continuar nas linhas do
   painel **Inventário**, dentro das molduras, sem atravessar a barra de dicas. Tire uma
   captura logo ao abrir e outra após cinco segundos.
2. Passe o mouse por vários espaços, clique, arraste, divida e equipe um item. Ícone,
   quantidade e durabilidade devem aparecer sobre a textura do slot; nenhum destaque deve
   permanecer depois que o mouse sair. Confira que os itens não mudaram de quantidade sem
   sua ação.
3. Troque de aba com Q/E, volte ao inventário e repita o passo 2. Depois injete falha em
   `win.inventory` pelo F8, reative e repita. O inventário vanilla deve voltar na falha;
   o tema deve voltar funcional na reativação, sem hover preso.
4. Confira que janela e barra de dicas ocupam cerca de 75% da tela e não cobrem os slots.
   Se o arquivo de configuração vinha da preview.5, `[Windows] Width` e `Height` devem
   passar uma vez de `0,65` a `0,75`.
5. Gaste e recupere vigor. Na barra horizontal, a linha quente deve ficar na **vertical**
   na ponta do líquido, com faíscas para a direita; o líquido e o rastro devem se mover
   suavemente. Faça uma captura ou vídeo curto durante o gasto.
6. Observe comida e status lado a lado: os ícones de status devem usar o mesmo quadro fino
   da comida. Abra o F8, grave um relatório e confirme a extensão `.log`. Envie o relatório,
   `LogOutput.log` e capturas se qualquer passo divergir.

Os controles finais de Equipamento, Criação e baús ainda pertencem às próximas etapas.

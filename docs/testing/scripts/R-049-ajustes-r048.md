# Roteiro R-049 — Ajustes após o teste do inventário

| Campo | Valor |
|---|---|
| Pacote | `GenesisMods-GenesisUI-0.7.0-preview.5.zip` |
| Pré-requisito | Backup do personagem de teste; usar o perfil de testes do Valheim. |

1. Abra o inventário em 1920×1080. A janela deve ocupar aproximadamente 65% da largura e
   altura, com barra superior mais baixa. Os três painéis devem caber sem encostar no HUD.
   Faça um print da tela inteira.
2. Confira os espaços do inventário com itens e vazios: cada um deve ter a moldura fina nova;
   ícone, número e durabilidade devem ficar sobre ela. Arraste, divida e equipe um item;
   conte os itens antes e depois.
3. Clique em **Todos**. A lista de oito categorias deve abrir. Escolha **Armas**: os outros
   itens devem ficar esmaecidos, mas ainda arrastáveis. Feche e abra o inventário e confira
   que o filtro continua clicável. Se falhar, envie o log com `filter list` e `filter selected`.
4. Confira a barra de abas, os três títulos e a separação entre abas: sem divisores altos,
   losangos no meio das laterais ou ornamento sobre o título. Teste as abas com Q/E e clique.
5. Gaste vigor: a barra horizontal deve ser maior, usar a textura nova, mostrar uma faixa de
   queima curta e deixar apenas um rastro discreto. Faça print enquanto ela está parcial.
6. Observe vida, vigor e eitr verticais e a barra de vida de um inimigo. As molduras devem
   manter a proporção; a vida do inimigo deve ter líquido animado e não deixar cantos pretos.
7. Compare a nova hotbar de oito espaços com a preview.4. O equipado deve ser dourado, sem
   verde. Informe se prefere a nova moldura ou os oito espaços finos da preview.4.
8. Abra um baú, mude de aba, feche o inventário, use F8 para injetar falha em `win.inventory`
   e reative. O inventário do jogo deve voltar inteiro em cada saída/falha. Envie o relatório
   do F8 e `LogOutput.log` se houver diferença.

Os painéis de equipamento e criação ainda não têm seus controles finais neste pacote.

# Roteiro R-032 — Terreno do minimapa, líquido das barras e corrida

| Campo | Valor |
|---|---|
| Pacote | `GenesisMods-GenesisUI-0.4.1-preview.2.zip` |
| Tempo estimado | ~15 min |
| Pré-requisito | R-000 no mesmo pacote |

## O que estamos testando

Se o terreno do minimapa aparece corretamente durante o jogo, se as bolhas das
barras são maiores e translúcidas, e se a nova barra de corrida acompanha o vigor.

## O que NÃO estamos testando

- Poder do guardião, conteúdo de outros mods e módulos restantes da F3.

## Preparação

1. Use o perfil local de teste com BepInExPack, Jötunn e este pacote GenesisUI;
   deixe outros mods desligados.
2. Entre em um mundo local com terreno já explorado e algum trecho ainda coberto
   pela névoa do mapa.
3. Deixe `[Modules] Minimap`, `Vitals` e `Sprint` ligados e `Scale = 1` nas seções
   `[Minimap]`, `[Vitals]` e `[Sprint]`.

## Passos

| # | Faça | Esperado | Se for diferente, anote |
|---|---|---|---|
| 1 | Assim que entrar, observe o minimapa e abra o mapa grande com `M` para comparar. | O terreno conhecido tem cor e detalhes nos dois mapas; áreas não exploradas continuam cobertas. O minimapa é redondo e os ícones estão dentro do aro. | Print `R-032-01.png` com os dois estados. |
| 2 | Ande pelo mundo por pelo menos 10 minutos; abra e feche o mapa grande algumas vezes. | O terreno do minimapa nunca fica cinza; posição, ícones e névoa acompanham o mapa do jogo. | Hora aproximada e print `R-032-02.png` se falhar. |
| 3 | Olhe as barras de vida e vigor de perto, parado e enquanto o vigor muda. | Bolhas reconhecíveis sobem devagar, são **bem translúcidas**, sem esconder os números; um reflexo pequeno se move na superfície e reage discretamente quando o valor muda. | Print `R-032-03.png` e diga se o efeito ficou forte ou fraco. |
| 4 | Corra para a frente segurando `Shift` por alguns segundos e depois pare. | Uma barra horizontal de VIGOR aparece acima dos itens, cai com a corrida e some suavemente cerca de 1,5 s após parar. Ela não cobre a barra de itens nem exige outra tecla para correr. | Print `R-032-04.png` durante a corrida e anote o comportamento ao parar. |
| 5 | No fim, abra o painel com `F8` e clique em **Gravar relatório**. | O relatório mostra `hud.sprint: Active`, sem falha inesperada; o log do minimapa informa `direct circular map mesh` e os nomes das texturas do mapa. | Envie o relatório e os logs mesmo se tudo passar. |

## Critério de aprovação

Todos os passos esperados confirmados e nenhuma falha inesperada do GenesisUI no
log. A correção do terreno só é considerada confirmada após este teste no cliente.

## O que me enviar

O relatório do F8, `BepInEx/LogOutput.log`, os prints indicados e uma frase sobre
o acabamento das barras e da nova barra de corrida.

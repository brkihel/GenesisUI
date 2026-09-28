# Roteiro R-041 — Ajustes do R-040 (curto)

| Campo | Valor |
|---|---|
| Pacote | `GenesisMods-GenesisUI-0.5.0-preview.4.zip` |
| Tempo estimado | ~10 min |
| Pré-requisito | Configuration Manager instalado (para o passo 6) |

Pode rodar junto com o primeiro teste da F4; são só as correções do R-040.

| # | Faça | Esperado | Se for diferente, anote |
|---|---|---|---|
| 1 | Pegue vários itens seguidos do chão (ex.: colha frutas, quebre pedras). | Até **3 cartões** no canto superior esquerdo, o mais novo em cima; cada um fica ~4 s e sai **caindo com fade**, dando lugar aos outros. Pegar o mesmo item de novo logo em seguida atualiza o cartão de cima ("x5", "x10") em vez de empilhar. | Print `R-041-01.png`. |
| 2 | Corra até gastar bastante vigor. | Na barra de vigor, a queima é só uma **borda curta brilhante** na superfície, com brasas; não cresce como um rastro longo. | Anote. |
| 3 | Olhe as barras de perto. | Poucos **fragmentos pequenos**, bem sutis, num tom mais escuro do líquido, dentro dele (sem brilho por cima). | Print `R-041-03.png`. |
| 4 | Deixe a vida abaixo de 25 %. | A **moldura** da barra de vida pulsa em vermelho, bem visível; nenhum quadrado vermelho em volta. | Print `R-041-04.png`. |
| 5 | Numa pedra de chefe (Vegvísir), aperte E para abrir o mapa. | O mapa grande abre **sem o cartão de interação por cima**. Feche o mapa: o cartão volta se você ainda estiver mirando a pedra. | Print `R-041-05.png`. |
| 6 | No F8, **Injetar falha** em `hud.vitals` e **Reativar**. Depois `[Vitals] ShowValues = false` e `true` no F1. | Ao reativar, as barras voltam **normais** (sem listras). Os números somem e voltam na hora. | Print se aparecer listra. |
| 7 | **Gravar relatório** no F8. | Módulos `Active`; no log, uma linha `HUD root placed below the large map` ou `large map is outside the HUD root`. | Me mande o relatório e o `LogOutput.log`. |

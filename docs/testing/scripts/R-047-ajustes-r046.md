# Roteiro R-047 — Ajustes do R-046

| Campo | Valor |
|---|---|
| Pacote | `GenesisMods-GenesisUI-0.7.0-preview.3.zip` |
| Tempo estimado | ~10 min |

As janelas de inventário, criação etc. **continuam as do jogo** neste pacote (a próxima entrega
monta o inventário do concept). Aqui só os ajustes que você pediu.

| # | Faça | Esperado | Se for diferente, anote |
|---|---|---|---|
| 1 | Olhe o HUD de dia. | Dourado **bem mais escuro e discreto** em tudo; barra de itens com **8 espaços finos soltos**, sem placa grossa; comida com o mesmo espaço fino. | Print `R-047-01.png`. |
| 2 | Equipe uma arma e (com controle) selecione outro espaço. | Equipado: espaço fino **verde**; selecionado: espaço fino **dourado aceso**. | Print. |
| 3 | Olhe o minimapa. | Placa de cima (vento + dia/hora) **igual à do bioma** embaixo, **por cima** da borda do anel. | Print `R-047-03.png`. |
| 4 | Fique com 3 ou mais efeitos (molhado, descansado, poder). | Quadros **mais próximos** entre si; nomes sem se encostar. | Print. |
| 5 | Abra o inventário. | A barra de abas **sem o título**, pontas e adornos **na proporção certa** (sem esticar). Vida, vigor, barra de itens, minimapa e as dicas do jogo **somem com fade**; ao fechar, **voltam com fade**. | Print aberto e fechado. |
| 6 | Na barra, vá até **Mapa** (E) e aperte **Q** ou **E** com o mapa aberto. | Volta para o inventário na aba vizinha. | Anote. |
| 7 | F8 → **Gravar relatório**. | No log: `window canvas scale 0.5` (ou outro número; me diga qual). | Mande o relatório e o `LogOutput.log`. |

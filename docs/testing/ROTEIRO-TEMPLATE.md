# Roteiro R-XXX — <título curto>

> Modelo. Copie para `docs/testing/scripts/R-XXX-<slug>.md` e preencha.
> Roteiros são em português: quem executa é o Diego, no cliente dele.

| Campo | Valor |
|---|---|
| Pacote | `GenesisMods-GenesisUI-x.y.z-preview.N.zip` |
| Tempo estimado | ~NN min |
| Pré-requisito | roteiros que precisam ter passado antes (ex.: R-000) |

## O que estamos testando

Uma frase. Ex.: "Que as barras de vida, vigor e eitr do GenesisUI mostram os mesmos
valores do jogo e voltam ao visual original quando o módulo é desligado."

## O que NÃO estamos testando

- Itens fora do escopo deste roteiro, para ninguém gastar tempo neles.

## Preparação

1. Perfil de mods **separado** do perfil de jogo, com só: BepInExPack, Jötunn
   <versão>, GenesisUI <versão> (e mais nada, salvo se o
   roteiro pedir).
2. Mundo local de teste (single-player). Nome sugerido: `GenesisUI-Teste`.
3. Configurações que precisam estar em valores específicos (arquivo, seção, chave,
   valor).
4. Abra o jogo com o console do BepInEx visível, se possível.

## Passos

| # | Faça | Esperado | Se for diferente, anote |
|---|---|---|---|
| 1 | Ação exata, com teclas e cliques | Resultado observável e objetivo | O que apareceu no lugar + print `R-XXX-01.png` |
| 2 | ... | ... | ... |

Dicas de uso da camada de debug (quando o roteiro usar):
- `F8` abre o painel de diagnóstico.
- `Ctrl+Alt` + passar o mouse mostra de qual módulo é um elemento.
- "Injetar falha" fica no painel Módulos.

## Critério de aprovação

- Todos os passos com "Esperado" confirmado, e
- nenhuma linha `[Error]` de `GenesisUI` no log da sessão.

## O que me enviar

1. O relatório: painel de diagnóstico → **Copiar relatório** (o caminho do arquivo
   vai para a área de transferência).
2. `BepInEx/LogOutput.log` e a pasta `BepInEx/GenesisUI/logs/`.
3. Os prints com o nome indicado em cada passo que falhou.
4. Em uma linha: passou / não passou, e o que mais chamou atenção.

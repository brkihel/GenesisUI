# Roteiro R-000 — Fumaça: o GenesisUI carrega, se identifica e não mexe em nada

| Campo | Valor |
|---|---|
| Pacote | `GenesisMods-GenesisUI-0.1.0-preview.1.zip` |
| Tempo estimado | ~15 min |
| Pré-requisito | nenhum (é o primeiro roteiro) |

## O que estamos testando

Que a versão de teste 0.1.0-preview.1 **carrega no seu cliente sem erro**, **diz quem
é** (marca d'água na tela e cabeçalho no log), **grava um relatório de diagnóstico que
esconde seus dados pessoais** e **não muda nada no jogo**.

## O que NÃO estamos testando

- Nenhuma parte visual nova: esta versão ainda não tem módulos. A interface do jogo
  deve continuar **exatamente** a de sempre.
- Servidor e multiplayer: tudo aqui é em mundo local.
- Desempenho.

## Preparação

1. No gerenciador de mods, crie um **perfil novo**, por exemplo `GenesisUI-Teste`, só
   com:
   - `BepInExPack_Valheim`
   - `Jotunn` **2.30.2** (a mesma versão do servidor)
2. Instale o GenesisUI no perfil:
   - **Pelo gerenciador:** "Importar mod local" e escolha o zip.
   - **À mão:** abra a pasta do perfil (no r2modman: *Settings → Browse profile
     folder*), crie `BepInEx/plugins/GenesisMods-GenesisUI/` e copie para lá o
     **conteúdo** da pasta `plugins/` do zip: o `GenesisUI.dll` e a pasta
     `Translations`.
3. Não precisa mexer em nenhuma configuração.
4. Tenha à mão a pasta do perfil (a mesma do item 2): os arquivos que vamos olhar ficam
   dentro de `BepInEx/`.

## Passos

| # | Faça | Esperado | Se for diferente, anote |
|---|---|---|---|
| 1 | Inicie o jogo pelo perfil `GenesisUI-Teste` e espere o **menu principal**. | No **canto inferior direito**, um texto dourado discreto: `GenesisUI PREVIEW 0.1.0-preview.1+xxxxxxx` (os x são o código da versão). | Print `R-000-01.png` do menu inteiro. |
| 2 | Sem fechar o jogo, abra `BepInEx/LogOutput.log` e procure por `[GenesisUI:Host]`. | Um bloco de linhas que começa com `GenesisUI 0.1.0-preview.1+… (Preview)` e traz `Game:`, `Unity:`, `BepInEx:`, `Jotunn: 2.30.2`, `Screen:`, `Plugins (N):` com a lista dos plugins, `own log file: …` e, por último, `ready`. **Nenhuma** linha `[Error]` que mencione `GenesisUI`. | Copie as linhas `[GenesisUI:…]` que estranhou. |
| 3 | Abra a pasta `BepInEx/GenesisUI/logs/`. | Existe um arquivo `genesisui-AAAAMMDD-HHMMSS.log` com as mesmas linhas do passo 2. | Anote se a pasta ou o arquivo não existirem. |
| 4 | Volte ao jogo, ainda no menu principal, e aperte **F8**. Depois abra `BepInEx/GenesisUI/reports/`. | Um arquivo `report-AAAAMMDD-HHMMSS.txt` novo. Se colar (Ctrl+V) no Explorer ou no Bloco de Notas, aparece o caminho desse arquivo. No menu **não** aparece mensagem na tela; isso é normal, porque o menu não tem HUD. | Anote se o arquivo não foi criado. |
| 5 | Crie um mundo local novo chamado `GenesisUI-Teste` (ou use um de teste) e entre com um personagem. | A marca d'água continua no canto inferior direito. **Todo o resto da interface está igual ao jogo sem mods**: vida, vigor, barra de itens, minimapa, bússola. | Print `R-000-05.png` com o HUD inteiro. |
| 6 | No mundo, aperte **F8**. | No **canto superior esquerdo** aparece: *"GenesisUI: relatório de diagnóstico salvo; o caminho foi copiado para a área de transferência."* (em inglês se o jogo estiver em inglês). Um segundo arquivo `report-….txt` surge na pasta. | Print `R-000-06.png`. |
| 7 | Abra o relatório do passo 6. | Primeira linha `=== GenesisUI diagnostic report … ===` e depois `(personal data redacted)`. Seções `Session`, `Faults` com `(none)`, `Patches` com `(none)`, `Input leases` com `active: 0`, `Config` e `Recent log`. **O nome do seu personagem aparece como `<character>`, o do mundo como `<world>`, e o seu usuário do Windows como `<os-user>` dentro dos caminhos.** | Anote qualquer dado pessoal que tenha aparecido. |
| 8 | Ande, pule, abra o inventário (**Tab**) e feche, abra o menu (**Esc**) e feche, abra o chat (**Enter**) e feche. | Tudo funciona como sempre; o mouse e o teclado nunca ficam presos. | Anote o que travou e em qual ação. |
| 9 | **Esc → Sair** para voltar ao menu principal. | A marca d'água aparece **uma única vez** no menu (não duplicada). | Print `R-000-09.png`. |
| 10 | Entre de novo no mesmo mundo, espere carregar e saia outra vez para o menu. | Continua uma única marca d'água. No `LogOutput.log`, nenhuma linha `[Error]` com `GenesisUI`. | Anote as linhas de erro, se houver. |
| 11 | Feche o jogo. Abra o arquivo de log do passo 3. | A última linha do GenesisUI é `[GenesisUI:Host] shutting down`. | Anote se ela não estiver lá. |
| 12 | Abra `BepInEx/config/Genesis.GenesisUI.cfg`. | Existem as seções `[General]` (com `Enabled`) e `[Diagnostics]` (com `DiagnosticsKey = F8` e `RedactReports = true`), com as descrições em português. | Anote o que faltou. |

## Critério de aprovação

- Todos os passos com o "Esperado" confirmado, e
- nenhuma linha `[Error]` de `GenesisUI` no `LogOutput.log` da sessão.

## O que me enviar

1. O relatório do passo 6 (`BepInEx/GenesisUI/reports/report-….txt`). Ele já vem sem os
   seus dados pessoais.
2. O arquivo `BepInEx/GenesisUI/logs/genesisui-….log` e o `BepInEx/LogOutput.log`.
   **Atenção:** esses dois **não** são filtrados e podem conter o nome do personagem e
   o seu usuário do Windows nos caminhos.
3. Os prints dos passos que falharam, com os nomes indicados.
4. Em uma linha: passou ou não passou, e o que mais chamou atenção.

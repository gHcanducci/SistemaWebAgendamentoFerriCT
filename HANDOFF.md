# Handoff — Integração Mercado Pago

**Branch atual:** `feat/mercado-pago-integration`
**Última sessão:** 2026-05-27
**Status:** Integração MP end-to-end validada. Design system Signal Yellow + P0 responsividade pushed. **Etapa 1 do plano dos requisitos do professor concluída nesta sessão** — P1 (cards mobile), P2 (@@media nas 6 views Admin restantes), Perfil mobile + botão Efetuar Pagamento, footer responsivo, F5 → Home/Index, todos commitados. Faltam etapas 2-6 do plano.

## Para retomar na próxima sessão

```powershell
git checkout feat/mercado-pago-integration
git pull               # sincroniza commits da sessão 2026-05-27
git status             # deve estar limpo
```

E diz pro Claude: **"continua do plano em `C:\Users\User\.claude\plans\d-user-downloads-requisitos-do-professor-curried-bird.md` a partir da etapa 2"** (hero da home no mobile) ou aponta a etapa que quiser priorizar.

Plano completo dos 5 requisitos do professor está salvo em:
`C:\Users\User\.claude\plans\d-user-downloads-requisitos-do-professor-curried-bird.md`

---

## 🎨 Sessão 2026-05-26 — Validação visual + responsividade

Antes de polir interfaces, o usuário pediu validação visual do refresh Signal Yellow (commit `e6ba3c5` que estava local). O processo identificou problemas reais de responsividade no painel admin e a sessão acabou fazendo P0 inteiro + P1 inteiro.

### O que foi commitado e pushed nesta sessão

| Commit | Resumo |
|---|---|
| `751683c` | **fix: responsividade do painel admin no mobile (P0)** — padding do `.admin-content` 28px → 16px/12px nos breakpoints 768/480px; topbar com `white-space: nowrap` + `flex-shrink: 0`; textos "Novo Aluno" e "Sair" somem em <480px deixando só ícone; `.cal-day-num` 3rem → 2.2rem; `.gitignore` ganhou entradas pra ignorar `.verify-evidence/` e `package*.json` (andaime de teste com Playwright) |

Antes desse commit, o `e6ba3c5` (Signal Yellow) também foi pushed — estava local desde 2026-05-25.

### O que **NÃO foi commitado ainda** (P1)

Duas views modificadas, validadas visualmente, esperando commit:

```
M  SistemaWebAgendamentoFerriCT/Views/Admin/Agendamentos.cshtml
M  SistemaWebAgendamentoFerriCT/Views/Admin/Alunos.cshtml
```

**`Agendamentos.cshtml`** — CSS-only transformação de tabela em cards:
- Adicionado `data-label="..."` em cada `<td>` (Aluno, Data, Horário, Tipo, Status, Solicitado, Ações)
- Substituído `style="display:flex;gap:6px"` inline por classe `.row-actions`
- Nova `@@media (max-width: 768px)`:
  - `thead` escondido; cada `<tr>` vira card com border + padding
  - Cada `<td>` fica `display: flex` com label à esquerda via `::before { content: attr(data-label) }` e valor à direita
  - "Aluno" vira header do card (sem label, com border-bottom)
  - "Ações" vira rodapé centralizado (border-top, botões 36×36)

**`Alunos.cshtml`** — reuso do `#gridView` que já existia (era toggle manual desktop):
- Botão Editar do card ganhou `<span>Editar</span>` ao lado do ícone (antes era só lápis esticado feio)
- Removido `style="flex:1;width:auto"` inline → virou classe `.card-btn-edit` no CSS
- Nova `@@media (max-width: 768px)`:
  - `#tableView { display: none !important }`
  - `#gridView  { display: block !important }`
  - `.view-toggle { display: none }` (toggle Tabela/Cards some em mobile)
  - `.cards-grid { grid-template-columns: 1fr }` (1 coluna)
  - botões do `.card-actions` ganham 40×40
  - `.toast-msg` reposicionado pra ocupar largura total

### Validação visual com Playwright (Edge headless)

Foi instalado `playwright-core` localmente (em `package.json`/`node_modules/`, agora ignorados) e criados scripts em `.verify-evidence/drive*.js` que:
- Compilam o projeto via `MSBuild` da VS 2022 Community
- Sobem IIS Express em background (porta 53302)
- Fazem login como admin (`admin/123`) e cliente (`demo@ferrict.com.br` + CPF `529.982.247-25`)
- Tiram screenshots em viewports 375/390/480/768/1366
- Param o IIS Express ao fim

Todo esse andaime está em `.verify-evidence/` que **está no .gitignore** — não vai pro git.

### Problemas detectados que **NÃO** são regressão

Durante a validação, dois "achados" ficaram registrados mas **não exigem ação**:

1. **"Buraco preto" na home em screenshot fullPage** — `Views/Home/Index.cshtml:393` define `.reveal { opacity: 0 }` que vira `.visible` via IntersectionObserver no scroll. Playwright `fullPage: true` não dispara o IO, então as seções abaixo do hero ficam invisíveis no PNG. **Funciona perfeito em browser real** — é artefato de captura, não bug.
2. **`/Agendamento/ListaEspera` e `/Agendamento/Retorno` sem `id`** → 500. Esperado — actions exigem `id`, só são atingíveis via fluxo de pagamento real.

---

## 📋 Backlog pendente pós-sessão

Em ordem de prioridade pra demo de **03/06/2026** — etapas restantes do plano dos requisitos do professor:

1. ✅ **Etapa 1 — consolidar uncommitted** (P1 + P2 + Perfil + footer + F5 fix) — concluída em 2026-05-27, 3 commits
2. 🟠 **Etapa 2 — hero da home no mobile**: números 8+/500+/100%/4 sobre foto da academia com contraste fraco em mobile. Em `Views/Home/Index.cshtml`.
3. 🟠 **Etapa 3 — CSS externo (escopo médio)**: extrair design system do `_Layout.cshtml` pra `Content/theme.css` + 5 views-chave (Home/Index, Cliente/Perfil, Admin/Index, Agendamento/Retorno, Agendamento/Pagamento). Atualizar `BundleConfig.cs`.
4. 🟠 **Etapa 4 — `@media print`**: criar `Content/print.css` global (esconde nav/footer/botões) + dedicado em `Agendamento/Retorno.cshtml` (comprovante) + botão "Imprimir Comprovante".
5. 🟠 **Etapa 5 — Acessibilidade mínima**: `aria-label` em 8 botões só-ícone, `aria-hidden="true"` em ícones decorativos, normalizar font-size mínimo 0.72rem, ajustar `--text-muted` pra contraste WCAG AA.
6. 🟢 **Etapa 6 — Re-validar PC externo na véspera (02/06)** num PC limpo: smoke test end-to-end MP sandbox.

---

## 🟢 Sessão 2026-05-27 — Etapa 1 do plano + plano completo escrito

### Trabalho do dia

- **Cards mobile no Perfil do cliente** (`Views/Cliente/Perfil.cshtml`) — `<table class="ag-table">` vira cards em ≤768px (data destacada em amarelo no header, label↑/valor↓ no meio, ações no rodapé). Mesma estratégia CSS-only do P1 do Admin.
- **Botão "Efetuar Pagamento"** em `Views/Cliente/Perfil.cshtml` — coluna "Ações" nova na tabela, mostra botão amarelo `btn-pagar-ag` apontando pra `/Agendamento/Pagamento/{id}` somente quando `Status == "PendentePagamento"`. Permite cliente retornar ao fluxo se sair da tela inicial.
- **Bug encoding UTF-8** — `Perfil.cshtml` foi reescrito via `Write` (sem BOM), MVC 5 interpretou como Windows-1252 e mojibake (`HORÁRIO` → `HORÃ¡RIO`, em-dash → `ã€"`). Regravado com BOM `EF BB BF` via PowerShell. Memória salva: [[feedback-cshtml-utf8-bom]].
- **Footer responsivo** (`Views/Shared/_Layout.cshtml`) — `.ferri-footer` virou flex container: copyright + Admin lado-a-lado em desktop com `space-between`, empilhados centralizados em ≤768px. Padding/fonte reduzidos em mobile. `.footer-admin-link` virou `inline-flex` + `white-space: nowrap`. Opacidade do Admin subiu pra 0.6 em mobile.
- **F5 → Home/Index** (`SistemaWebAgendamentoFerriCT.csproj.user`) — `<StartAction>` mudou de `CurrentPage` (abria a view aberta no editor) pra `Project` (abre `/`). Arquivo é local, não trackeado no git.
- **6 views Admin com `@@media`** (P2) — `CriarAluno`, `EditarAluno`, `EditarAgendamento`, `RegistrarPagamentoManual`, `ExcluirAluno`, `ExcluirAgendamento`. Padrão: `@@media (max-width: 768px)` reduz `.page-title` 2rem→1.6rem; `@@media (max-width: 480px)` empilha `.form-actions`/`.confirm-actions`/`.pgm-actions` como coluna, grids 3-colunas (`.status-options`, `.pgm-forma`) viram 1 coluna, `.info-row` quebra label↑/valor↓.
- **Bug CSS no CriarAluno** — descobri 2 ocorrências de `media (max-width: ...)` (sem `@@` e sem `@` simples) — CSS completamente quebrado. Corrigido pra `@@media`.
- **Plano completo escrito** — `C:\Users\User\.claude\plans\d-user-downloads-requisitos-do-professor-curried-bird.md` cobre os 5 requisitos do professor com decisões já tomadas (CSS externo escopo médio, @media print global + foco no comprovante).

### Commits da sessão 2026-05-27

| Commit | Resumo |
|---|---|
| (P1) | **fix: tabelas Admin viram cards no mobile (P1)** — `Agendamentos.cshtml` + `Alunos.cshtml` |
| (P2) | **fix: @@media nas views Admin sem responsividade (P2)** — 6 views Admin + bug fix `media → @@media` no `CriarAluno` |
| (perfil/footer) | **fix: Perfil mobile + botão Efetuar Pagamento + footer responsivo + F5 → Home** — `Perfil.cshtml` + `_Layout.cshtml` + `.csproj.user` (não trackeado, fica local) + atualização do `HANDOFF.md` |

### Aprendizados desta sessão (memórias salvas)

- [[feedback-cshtml-utf8-bom]] — `.cshtml` deste projeto exige UTF-8 com BOM ou acentos viram mojibake. `Write` padrão grava sem BOM; após reescrita, regravar com BOM via PowerShell.
- [[feedback-pedir-aprovacao]] — não pedir aprovação repetida antes de aplicar Edits locais já alinhados.

---

## 🎯 Objetivo da fase atual (definido 2026-05-22)

**Apresentação para professor em 03/06/2026.** O professor vai rodar o projeto **no próprio PC dele**, via `git clone` + F5 no Visual Studio.

Restrições do cenário:
- Não dá pra depender de configuração manual no PC do professor (Web.secrets.config, login MP, cadastro de webhook).
- Tem que ser fácil: idealmente, 1 comando de PowerShell + F5.
- Deve rodar **end-to-end com MP real** (não Mock — rejeitado pelo usuário em 2026-05-22).

### Estratégia escolhida

| Obstáculo | Solução |
|---|---|
| Webhook do MP precisa de URL pública | **ngrok com subdomínio reservado grátis** — URL fixa permanente |
| Credenciais MP estão em `Web.secrets.config` gitignored | **Commitar credenciais TEST direto no `Web.config`** — são SANDBOX, não cobram dinheiro real |
| Webhook MP tem que estar cadastrado na URL atual | URL fixa do ngrok → cadastrar **uma única vez** no painel MP |
| Setup operacional toda vez que liga | Script `start-demo.ps1` que sobe ngrok + abre VS |

⚠️ **NÃO foi escolhido o Gateway Mock** (foi proposto e rejeitado — usuário quer MP real funcionando).

---

## 📋 Tasks da fase atual

| # | Task | Status |
|---|---|---|
| 2 | Seed com 2 clientes de demo | ✅ Feito (CPF formatado: `529.982.247-25`, `111.444.777-35`) |
| 3 | README de instalação em PC novo | ✅ Feito (`README.md`) |
| 4 | ngrok com subdomínio reservado | ✅ Feito (`molehill-salvation-clothes.ngrok-free.dev`) |
| 5 | Credenciais TEST commitadas no `Web.config` | ✅ Feito (com comentário explicando justificativa) |
| 6 | Script `start-demo.ps1` | ✅ Feito (inclui `--host-header=rewrite`) |
| 7 | Validar fluxo end-to-end | ✅ Feito 2026-05-24 (Saldo em conta) |

## ⚠️ Lições aprendidas em 2026-05-24

### 1. `--host-header=rewrite` é OBRIGATÓRIO no ngrok

Sem essa flag, o IIS Express rejeita as requests vindas via ngrok com **"400 Bad Request - Invalid Hostname"**. O ngrok mantém o header `Host: molehill-salvation-clothes.ngrok-free.dev` por padrão, e o IIS Express só aceita `localhost`. A flag faz o ngrok reescrever pra `localhost:44358` antes de encaminhar.

Comando correto:
```powershell
ngrok http https://localhost:44358 --domain=molehill-salvation-clothes.ngrok-free.dev --host-header=rewrite
```

Já incorporado no `start-demo.ps1`.

### 2. ngrok grátis mostra interstitial "ERR_NGROK_6024" pra browsers

Primeira visita ao domínio em cada sessão de navegador mostra tela de aviso anti-abuse do ngrok. Click em "Visit Site" → cookie grava → não aparece mais. **Webhooks (server-to-server) NÃO passam por essa tela.**

**Pro dia da demo:** abrir `https://molehill-salvation-clothes.ngrok-free.dev` no navegador do professor 5min antes e clicar "Visit Site". Documentar isso no `DEMONSTRACAO-MERCADO-PAGO.md`.

### 3. Test buyer sandbox só mostra meios de pagamento salvos

Tela do MP não dá opção "Cartão novo" — só os meios já salvos do test buyer. No nosso caso aparecem:
- **Saldo em conta** (R$ 142,50 inicial, gastos R$ 50/transação) — `account_money`
- **Cartão de Débito Virtual CAIXA** — apareceu mas rejeitou o cartão de teste Mastercard `5031 4332 1540 6351` (divergência de bandeira)

**Caminho que funciona pra demo:** Saldo em conta.

### 4. CPF em comparação exata, formato com pontuação no banco

Login do cliente (`ClienteController.cs:38`) faz `c.CPF == vm.CPF` sem sanitizar. A view tem máscara JS que formata `529.982.247-25`. Seed precisa ter CPF formatado pra bater (já corrigido).

### Ordem de execução sugerida

✅ Todas concluídas em 2026-05-22 e 2026-05-24.

## 🎯 PRÓXIMA SESSÃO — preparar interface pra apresentação 03/06

**Decisão do usuário em 2026-05-24:** após validar end-to-end, fase atual passa a ser **polir a interface** pra apresentação ao professor. Os cenários pendentes da Task #9 (webhook forjado, ownership check, etc.) ficam pra depois — não bloqueiam a demo principal.

### O que provavelmente vai ser tocado

Sem brief detalhado ainda — o usuário vai abrir a próxima sessão dizendo o que quer melhorar. Possíveis frentes (esperar ele apontar antes de mexer):

- **Tela de Retorno** (`Views/Agendamento/Retorno.cshtml`) — primeira impressão pós-pagamento, deve estar polida
- **Tela de Pagamento** (`Views/Agendamento/Pagamento.cshtml`) — botão "Pagar com Mercado Pago"
- **Tela de Login** (`Views/Cliente/Login.cshtml`) — visualmente já tem CSS custom, mas pode precisar de ajuste
- **Painel admin** (`Views/Admin/`) — listagem de agendamentos, ação de pagamento manual
- **Index/Home** (`Views/Home/Index.cshtml`) — landing page
- **Tela de agendamento** (`Views/Agendamento/Create.cshtml` e listas)
- **Mobile responsivo** — academia provavelmente é acessada do celular pelos alunos
- **Dark mode** — vi que já tem variáveis CSS `var(--accent)`, `var(--bg-card)`, etc., sugere tema escuro

### Lembrar antes de mexer em CSS dentro de .cshtml

⚠️ **`@@` obrigatório no Razor:** toda diretiva CSS que começa com `@` (`@media`, `@keyframes`, `@font-face`) precisa ser escapada como `@@`. Sem isso, Razor tenta interpretar como código C# e o CSS é **silenciosamente quebrado**. Já documentado no `CLAUDE.md`.

### Estado do código pra retomar

- Branch: `feat/mercado-pago-integration` (3 commits à frente do origin + mudanças NÃO commitadas desta sessão)
- Arquivos modificados em 2026-05-22 e 2026-05-24 (não commitados):
  - `Migrations/Configuration.cs` — seed com 2 clientes de demo
  - `Web.config` — credenciais TEST do MP + URL ngrok fixa
  - `Web.secrets.config` — sincronizado com URL ngrok (local, gitignored)
  - `start-demo.ps1` — novo
  - `README.md` — novo
  - `HANDOFF.md` — atualizado (este arquivo)
- Decisão: usuário NÃO commitou ainda — quer fazer um único commit grande junto com as melhorias de interface, OU commitar em fase separada antes da próxima. **Perguntar antes de commitar.**

---

## 🔧 Detalhes técnicos por task

### Task 4 — ngrok com subdomínio reservado

- Conta gratuita em `ngrok.com`
- Plano grátis dá 1 endpoint estático reservado
- Comando esperado: `ngrok http https://localhost:44358 --domain=<subdominio>.ngrok-free.app`
- Precisa autenticar com `ngrok config add-authtoken <token>` (token na conta ngrok)
- Substituir todas as referências a Cloudflare nos docs

### Task 5 — Credenciais TEST no Web.config

Mover de `Web.secrets.config` (gitignored) para `Web.config` (commitado):

```xml
<add key="MercadoPago:AccessToken" value="APP_USR-..." />     <!-- test seller -->
<add key="MercadoPago:PublicKey" value="APP_USR-..." />       <!-- test seller -->
<add key="MercadoPago:WebhookSecret" value="..." />           <!-- chave secreta -->
<add key="MercadoPago:NotificationUrl" value="https://<subdominio>.ngrok-free.app/Pagamento/Webhook" />
<add key="MercadoPago:BackUrlBase" value="https://<subdominio>.ngrok-free.app" />
```

⚠️ **JUSTIFICATIVA (importante deixar em comentário no Web.config):**
- Credenciais são de **test seller** (sandbox MP), NÃO produção
- NÃO cobram dinheiro real, NÃO acessam dados sensíveis
- Decisão deliberada pra simplificar demo acadêmica
- Em produção real, deveriam voltar pra `Web.secrets.config` ou Azure Key Vault

Continuar mantendo `Web.secrets.config` no `.gitignore` por segurança geral (mas o arquivo pode nem existir mais).

### Task 6 — start-demo.ps1

Esqueleto previsto:
```powershell
# 1. Verifica ngrok instalado, baixa se não tiver
# 2. Sobe ngrok com subdomínio reservado em background
# 3. (opcional) Aguarda 3s pra ngrok subir
# 4. (opcional) Abre Visual Studio com .sln
# 5. Mostra na tela: "Agora aperte F5 no Visual Studio"
```

### Task 2 — Seed de demo

Auditar `Migrations/Configuration.cs`. Confirmar que após F5 inicial:
- Existe cliente teste com login conhecido (anotar email + senha no README)
- Existem professores cadastrados
- Existem turmas com horários populados
- Admin login default funciona (atualmente SHA-256 de "123" — dívida técnica conhecida)

### Task 3 — README de instalação

Conteúdo mínimo:
- Pré-requisitos: .NET Framework 4.x, SQL Server LocalDB, Visual Studio 2019+, ngrok (opcional pra ver webhook funcionando)
- Comandos: `git clone`, `nuget restore`, F5
- Credenciais admin default
- Credenciais cliente demo default
- Como rodar `start-demo.ps1` pra ativar túnel
- O que esperar do MP em modo demo (cartão de teste, test buyer)
- Como alternar pra credenciais reais (caso queira fazer testes próprios)

---

## ✅ O que já está pronto (snapshot de 2026-05-19)

Todo o trabalho técnico de integração MP — Tasks 1-8 do plano original — está completo e validado:

1. ✅ Setup MP + túnel HTTPS
2. ✅ Migration `AddMercadoPagoFields`
3. ✅ `MercadoPagoService`
4. ✅ Refactor `AgendamentoController`
5. ✅ Webhook + validação HMAC-SHA256
6. ✅ Página de retorno (bug Razor corrigido em `66d0b52`)
7. ✅ Job de cleanup automático (timeout 1h)
8. ✅ Pagamento manual admin
9. 🟡 Teste end-to-end no sandbox — **Débito validado**, outros cenários abaixo

### Cenários de teste ainda não exercitados (pendência da Task 9)

Quando o setup do ngrok estiver pronto, podem ser feitos de novo (URL não muda mais):

- [ ] Cartão recusado (CVV inválido / nome `OTHE`) — 2min
- [ ] Webhook forjado (POST sem `x-signature` → 401) — 5min, **segurança**
- [ ] Pagamento manual admin — 3min
- [ ] Ownership check (cliente A acessa agendamento B → 403) — 5min, **segurança**
- [ ] Timeout 1h (cleanup job) — espera longa
- [ ] PIX — só em produção (sandbox MP quebrado pra test sellers)

---

## 🔑 Aprendizados críticos preservados (das sessões anteriores)

### 1. Test users do MP

- Criar 2 test users no painel developers: **seller** e **buyer**
- Fazer **logout** da conta real, login com email/senha do test seller
- Dentro da conta dele, criar/abrir aplicação
- AccessToken e PublicKey aparecem como `APP_USR-...` (sandbox porque conta é de teste)

⚠️ NÃO confundir: `APP_USR-*` da conta REAL = produção. `APP_USR-*` do test seller = sandbox.

### 2. WebhookSecret ≠ AccessToken

Quando cadastra webhook no painel, MP gera chave secreta específica do webhook. Vai em `MercadoPago:WebhookSecret`.

Se recadastra o webhook (ou troca de aplicação), MP gera nova chave. Tem que atualizar ou todos os webhooks retornam 401.

### 3. Simulação de webhook do painel sempre dá 500

Não é bug nosso. Simulador manda `data.id: "123456"` (ID falso). HMAC passa, mas busca na API real do MP retorna 404 → 500.

**Como saber se HMAC funcionou:** Output do VS deve dizer `Webhook MP: falha ao consultar payment 123456` (HMAC OK). Se aparecer `assinatura inválida` → WebhookSecret errado.

### 4. PIX em sandbox quebrado pra test sellers

Cadastro de chave PIX retorna `PKF03-VYL8CGTZPHCP`. Validar PIX só em produção com R$ 0,50-1,00.

### 5. account_money não dá pra excluir

MP retorna 400 "account_money cannot be excluded". Comprador vê "Saldo em conta" se tiver. Aceitável.

### 6. Cartões de teste pra Débito

- Aprovado: `5031 4332 1540 6351`, CVV `123`, validade futura, nome `APRO`
- Recusado: mesmo cartão, nome `OTHE`

---

## 🚧 Dívidas técnicas conhecidas

1. **`SenhaAdminHash` hardcoded** (SHA-256 de "123") em `AdminController`. Em produção, mover pra `Web.secrets.config` e usar bcrypt/Argon2.
2. **`FormaPagamento` em `Pagamento` ainda é `[Required]`** mas usamos placeholder `"Aguardando"`. Aceitável.
3. **`MapearFormaPagamento`** não trata `prepaid_card` — retorna `"Aguardando"`. Como bloqueamos via Preference, baixa prioridade.
4. **`AgendamentoCleanupJob` usa `Timer` estático** — em IIS com app pool recycle pode perder ticks. Considerar Hangfire em produção.
5. **Sem logging estruturado** — usando `System.Diagnostics.Trace`. NLog/Serilog em produção.
6. **Sem testes automatizados** — prioridade alta: `WebhookSignatureValidator` (HMAC).
7. **PIX em produção não foi validado ainda**.
8. **Credenciais TEST no Web.config após Task 5** — em produção, devem voltar pra secrets externos.

---

## 💰 Defesas de segurança implementadas (20 itens — preservar)

| # | Defesa | Local |
|---|---|---|
| 1 | Access Token em secrets (será Web.config após Task 5, mas é sandbox) | `Web.config` + `.gitignore` |
| 2 | HMAC-SHA256 do `x-signature` em **constant time** | `WebhookSignatureValidator.cs` |
| 3 | Tolerância de timestamp 5min (anti-replay) | `WebhookSignatureValidator.MaxClockSkew` |
| 4 | `WebhookEventoId` UNIQUE filtrado (idempotência) | Migration + `PagamentoController.Webhook` |
| 5 | `CodigoTransacao` UNIQUE filtrado (anti-replay de paymentId) | Migration |
| 6 | `PreferenceId` UNIQUE filtrado | Migration |
| 7 | Re-busca via `/v1/payments/{id}` antes de confirmar | `PagamentoController.Webhook` step 5 |
| 8 | Validação `transaction_amount` == valor server-side | `PagamentoController.Webhook` step 7 |
| 9 | `X-Idempotency-Key` na criação de Preference | `MercadoPagoService.CriarPreferenceAsync` |
| 10 | `excluded_payment_types`: bloqueia crédito/prepaid/boleto/ATM/cripto | `MercadoPagoService.CriarPreferenceAsync` |
| 11 | Ownership check (cliente A não acessa B) → 403 | `AgendamentoController.Pagamento/IniciarPagamento/Retorno` |
| 12 | Guard "1 PendentePagamento por cliente" | `AgendamentoController.Create` POST |
| 13 | Pagamentos antigos cancelados ao recriar Preference | `AgendamentoController.IniciarPagamento` |
| 14 | Botão `disabled` ao submeter (anti duplo-click) | `Pagamento.cshtml` JS |
| 15 | Webhook **sem** `[ValidateAntiForgeryToken]` (defesa = HMAC) | `PagamentoController.Webhook` |
| 16 | Valor recalculado server-side em todo lugar | controllers |
| 17 | `[FiltroAcesso]` herdado em `RegistrarPagamentoManual` | `AdminController` |
| 18 | Forma de pagamento manual via whitelist | `AdminController.RegistrarPagamentoManual` POST |
| 19 | CPF sanitizado (só dígitos) antes de mandar pro MP | `MercadoPagoService.SomenteDigitos` |
| 20 | TLS 1.2 forçado no HttpClient | `MercadoPagoService.CriarHttpClient` |

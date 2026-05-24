# Handoff — Integração Mercado Pago

**Branch atual:** `feat/mercado-pago-integration`
**Última sessão:** 2026-05-24
**Status:** End-to-end **validado** com Saldo em conta + ngrok subdomínio fixo. Setup operacional pronto pra demo no PC do professor.

## Para retomar na próxima sessão

```powershell
git checkout feat/mercado-pago-integration
```

E diz pro Claude: "continua do HANDOFF.md".

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

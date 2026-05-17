# Handoff — Integração Mercado Pago

**Branch atual:** `feat/mercado-pago-integration`
**Última sessão:** 2026-05-17
**Status:** Código escrito e commitado. **Testes end-to-end no sandbox em andamento — travado em configuração de credenciais.**

## Para retomar na próxima sessão

```powershell
git checkout feat/mercado-pago-integration
```

E diz pro Claude: "continua do HANDOFF.md, vamos retomar Task #9".

---

## ⚠️ Bloqueio atual (onde paramos)

Durante o teste end-to-end (Task #9), descobrimos que a conta MP de produção do dev:
- **PIX precisou ser habilitado** em `mercadopago.com.br/cobrar-com-pix` (já feito — chave aleatória criada)
- **TEST tokens da aplicação MP foram trocados** durante a investigação por tokens de "test users" (caminho errado)

**Próxima ação concreta** — restaurar credenciais corretas:

1. Logar em `mercadopago.com.br/developers` com a **conta real** (não test user)
2. Entrar na aplicação `Ferri CT — Agendamento`
3. Navegar até **Credenciais de teste** (deve mostrar `TEST-...` AccessToken + PublicKey nessa página, possivelmente em uma aba específica)
   - Se MP só estiver mostrando "Test users", procurar aba "Access Token" / "Credentials" ao lado
   - Se realmente não existir, clicar em **Gerar credenciais de teste** (botão pode existir)
4. Copiar AccessToken (`TEST-...`) e PublicKey (`TEST-...`) corretos
5. Colar em `SistemaWebAgendamentoFerriCT/Web.secrets.config` nos campos `MercadoPago:AccessToken` e `MercadoPago:PublicKey`
6. **NÃO mexer** em `WebhookSecret`, `NotificationUrl`, `BackUrlBase`
7. Confirmar que a URL do túnel cloudflared ainda está ativa e que `NotificationUrl`/`BackUrlBase` batem (se diferente, atualizar nos dois — `Web.secrets.config` E painel Webhooks do MP)
8. Shift+F5 + F5 no Visual Studio
9. Testar de novo em janela anônima

## ⚠️ NÃO usar tokens com prefixo `APP_USR-` no `Web.secrets.config`

Esses são tokens de **produção** — cobram de verdade. Em desenvolvimento, sempre usar `TEST-*`.

---

## Resumo executivo

Implementadas 8 das 9 tasks do plano. A única pendente é teste manual no sandbox (Task #9), que depende do ambiente local com Cloudflare Tunnel + `Web.secrets.config` real.

### Sessão de teste 2026-05-17 — descobertas e fixes

Durante o teste end-to-end, foram identificados e corrigidos 2 bugs na configuração da Preference:

1. **`account_money` não pode ser excluído** — MP retornava 400 "account_money cannot be excluded". Fix: removido da lista de `excluded_payment_types` em `MercadoPagoService.cs`. Agora permite saldo MP Wallet (comportamento similar a PIX, baixo risco).

2. **`installments: 1` filtra débito em sandbox** — apesar de documentado como genérico, esse campo é tratado pelo MP como específico de crédito e estava removendo opções de débito. Fix: removido `installments` da Preference em `MercadoPagoService.cs`.

3. **Logging do erro real do MP** — `IniciarPagamento` no `AgendamentoController` agora loga `ex.StatusCode`, `ex.Message` e `ex.ResponseBody` via `Trace.TraceError`. Ajuda a debugar respostas de erro do MP via Output do VS.

**Configurações da conta MP feitas hoje:**
- PIX habilitado em `mercadopago.com.br/cobrar-com-pix` (chave aleatória cadastrada)
- Webhook URL cadastrada no painel MP apontando para o túnel Cloudflare atual

### Tasks concluídas

| # | Task | Arquivos chave |
|---|---|---|
| 1 | Setup MP + túnel HTTPS | `Web.config`, `Web.secrets.config.example`, `.gitignore` |
| 2 | Migration `AddMercadoPagoFields` | `Migrations/202605172019119_AddMercadoPagoFields.cs`, `Models/Pagamento.cs` |
| 3 | `MercadoPagoService` | `MercadoPago/MercadoPagoService.cs` + 5 outros em `MercadoPago/` |
| 4 | Refactor `AgendamentoController` | `Controllers/AgendamentoController.cs`, `Views/Agendamento/Pagamento.cshtml`, `ViewModels/PagamentoViewModel.cs` |
| 5 | Webhook + validação HMAC | `Controllers/PagamentoController.cs`, `MercadoPago/WebhookSignatureValidator.cs` |
| 6 | Página de retorno | `Views/Agendamento/Retorno.cshtml` + action `Retorno` em `AgendamentoController` |
| 7 | Job de cleanup automático | `Tasks/AgendamentoCleanupJob.cs`, `Global.asax.cs` |
| 8 | Pagamento manual admin | `Views/Admin/RegistrarPagamentoManual.cshtml` + 2 actions em `AdminController` |
| 9 | **Teste end-to-end no sandbox** | **Pendente — do lado do usuário** |

---

## Como continuar (próxima sessão)

### 1. Build do projeto

No Visual Studio: **Ctrl+Shift+B**. Se houver erro de compilação, ele indica algo que ficou pendurado de uma das tasks acima. Provavelmente algum `<Compile Include>` faltando no `.csproj` — eu adicionei todos os que precisava, mas confira.

### 2. Confirmar que `Web.secrets.config` existe e tem valores

Em `SistemaWebAgendamentoFerriCT/Web.secrets.config` (NÃO versionado), com:

```xml
<add key="MercadoPago:AccessToken" value="TEST-..." />
<add key="MercadoPago:PublicKey" value="TEST-..." />
<add key="MercadoPago:WebhookSecret" value="..." />
<add key="MercadoPago:NotificationUrl" value="https://<tunel>/Pagamento/Webhook" />
<add key="MercadoPago:BackUrlBase" value="https://<tunel>" />
```

### 3. Subir o túnel Cloudflare

```powershell
$env:GODEBUG = "netdns=cgo"
& "$env:USERPROFILE\cloudflared\cloudflared.exe" tunnel --url https://localhost:44358 --no-tls-verify --http-host-header localhost:44358
```

URL muda a cada execução. Atualize `Web.secrets.config` e webhook no painel MP se mudar.

### 4. Rodar o projeto (F5) e testar Task #9

Cenários a validar:

- [ ] **PIX aprovado:** cria agendamento → clica Pagar → escolhe PIX no MP → simula pagamento → webhook chega → status muda para Confirmado
- [ ] **Débito aprovado:** mesmo fluxo com cartão de teste do MP
- [ ] **Cartão recusado:** cartão de teste com CVV inválido → status Cancelado
- [ ] **Timeout 1h:** criar agendamento, NÃO pagar, esperar 1h05min → cleanup job marca Cancelado (logs em Output do VS)
- [ ] **Webhook duplicado:** MP reenvia o mesmo evento → segundo retorna `"Already processed"` (idempotência por `WebhookEventoId`)
- [ ] **Webhook forjado:** mandar POST sem `x-signature` (via curl/Postman) → 401
- [ ] **Pagamento manual admin:** painel admin → DetalhesAgendamento → URL `/Admin/RegistrarPagamentoManual/{id}` → forma=Dinheiro → status Confirmado
- [ ] **Lista de espera:** criar agendamento em turma lotada → fica `AguardandoVaga`, sem fluxo MP
- [ ] **Cliente A não acessa agendamento de B:** tentar `/Agendamento/Pagamento/{idDeOutroCliente}` → 403

### 5. Cartões/PIX de teste do MP (sandbox)

- **Cartão débito aprovado:** 5031 4332 1540 6351, CVV 123, validade qualquer futura. Nome `APRO` ou `OTHE` para outros cenários.
- **PIX:** o MP gera QR Code → clicar em "Aprovar pagamento" no painel sandbox.
- Documentação: https://www.mercadopago.com.br/developers/pt/docs/checkout-pro/test-integration

---

## Arquivos criados/modificados nesta sessão

### Novos
```
SistemaWebAgendamentoFerriCT/
├── MercadoPago/
│   ├── MercadoPagoSettings.cs
│   ├── MercadoPagoException.cs
│   ├── IMercadoPagoService.cs
│   ├── MercadoPagoService.cs
│   ├── PreferenceCreatedResult.cs
│   ├── PaymentInfo.cs
│   └── WebhookSignatureValidator.cs
├── Controllers/
│   └── PagamentoController.cs
├── Tasks/
│   └── AgendamentoCleanupJob.cs
├── Views/
│   ├── Agendamento/Retorno.cshtml
│   └── Admin/RegistrarPagamentoManual.cshtml
├── Migrations/
│   └── 202605172019119_AddMercadoPagoFields.{cs,Designer.cs,resx}
└── Web.secrets.config.example
```

E na raiz:
```
CLAUDE.md
HANDOFF.md (este arquivo)
```

### Modificados
```
SistemaWebAgendamentoFerriCT/
├── Web.config                       (appSettings file=Web.secrets.config + 5 keys MP)
├── Global.asax.cs                   (chama AgendamentoCleanupJob.Iniciar)
├── SistemaWebAgendamentoFerriCT.csproj  (Compile Includes dos novos arquivos)
├── Controllers/
│   ├── AgendamentoController.cs    (Refactor: IniciarPagamento async, Retorno, guards)
│   └── AdminController.cs          (RegistrarPagamentoManual + ValorMatricula consts)
├── Models/Pagamento.cs              (5 campos novos: PreferenceId, WebhookEventoId, etc)
├── ViewModels/PagamentoViewModel.cs (removido FormaPagamento)
└── Views/Agendamento/Pagamento.cshtml (refeita — botão MP em vez de form PIX/Cartão simulado)

.gitignore                           (ignora Web.secrets.config, mantém .example)
```

---

## Defesas de segurança implementadas (lista de verificação)

| # | Defesa | Local |
|---|---|---|
| 1 | Access Token em arquivo `Web.secrets.config` gitignored | `Web.config` + `.gitignore` |
| 2 | HMAC-SHA256 do `x-signature` em **constant time** | `WebhookSignatureValidator.cs` |
| 3 | Tolerância de timestamp 5min (anti-replay) | `WebhookSignatureValidator.MaxClockSkew` |
| 4 | `WebhookEventoId` UNIQUE filtrado (idempotência) | Migration + `PagamentoController.Webhook` |
| 5 | `CodigoTransacao` UNIQUE filtrado (anti-replay de paymentId) | Migration |
| 6 | `PreferenceId` UNIQUE filtrado | Migration |
| 7 | Re-busca via `/v1/payments/{id}` antes de confirmar | `PagamentoController.Webhook` step 5 |
| 8 | Validação `transaction_amount` == valor server-side (anti-tampering) | `PagamentoController.Webhook` step 7 |
| 9 | `X-Idempotency-Key` na criação de Preference | `MercadoPagoService.CriarPreferenceAsync` |
| 10 | `excluded_payment_types`: bloqueia crédito/boleto/ATM/wallet | `MercadoPagoService.CriarPreferenceAsync` |
| 11 | Ownership check (cliente A não acessa agendamento B) → 403 | `AgendamentoController.Pagamento`/`IniciarPagamento`/`Retorno` |
| 12 | Guard "1 PendentePagamento por cliente" | `AgendamentoController.Create` POST |
| 13 | Pagamentos antigos cancelados ao recriar Preference | `AgendamentoController.IniciarPagamento` |
| 14 | Botão `disabled` ao submeter (mitiga duplo-click) | `Pagamento.cshtml` JS |
| 15 | Webhook **sem** `[ValidateAntiForgeryToken]` (defesa = HMAC) | `PagamentoController.Webhook` |
| 16 | Valor recalculado server-side em todo lugar (nunca confiar no client) | controllers |
| 17 | `[FiltroAcesso]` herdado em `RegistrarPagamentoManual` | `AdminController` (class-level filter) |
| 18 | Forma de pagamento manual via whitelist (`Dinheiro`/`Pix`/`Debito`) | `AdminController.RegistrarPagamentoManual` POST |
| 19 | CPF sanitizado (só dígitos) antes de mandar pro MP | `MercadoPagoService.SomenteDigitos` |
| 20 | TLS 1.2 forçado no HttpClient | `MercadoPagoService.CriarHttpClient` |

---

## Pontos de atenção / dívidas técnicas

1. **`SenhaAdminHash` hardcoded** em `AdminController` (SHA-256 de "123"). Em produção, mover para `Web.secrets.config` e usar algoritmo mais robusto (bcrypt/Argon2 via NuGet).

2. **`FormaPagamento` em `Pagamento` ainda é `[Required]`** mas usamos string placeholder `"Aguardando"` enquanto não houver confirmação MP. Aceitável, mas poderia virar nullable em migration futura.

3. **Sem logging estruturado** — usando `System.Diagnostics.Trace`. Para produção, considerar NLog ou Serilog.

4. **Job de cleanup usa `Timer` estático** — em ambientes com app pool recycle agressivo (IIS), pode perder ticks. Para produção robusta, considerar Hangfire.

5. **Sem testes automatizados** — toda validação até aqui é manual. Sugestão de prioridade: testes do `WebhookSignatureValidator` (lógica criptográfica é onde bugs sutis machucam mais).

6. **A URL do túnel Cloudflare muda a cada execução** — em produção haverá domínio fixo. Em dev, lembre de atualizar webhook no painel MP + `Web.secrets.config` quando reiniciar.

7. **MP Sandbox às vezes demora** alguns segundos para enviar webhook após pagamento. A página `Retorno.cshtml` tem auto-refresh de 5s pra cobrir isso.

---

## Para commitar no fim da sessão atual

```bash
git add CLAUDE.md HANDOFF.md
git add SistemaWebAgendamentoFerriCT/Web.config
git add SistemaWebAgendamentoFerriCT/Web.secrets.config.example
git add SistemaWebAgendamentoFerriCT/Global.asax.cs
git add SistemaWebAgendamentoFerriCT/SistemaWebAgendamentoFerriCT.csproj
git add SistemaWebAgendamentoFerriCT/Controllers/
git add SistemaWebAgendamentoFerriCT/Models/Pagamento.cs
git add SistemaWebAgendamentoFerriCT/ViewModels/PagamentoViewModel.cs
git add SistemaWebAgendamentoFerriCT/MercadoPago/
git add SistemaWebAgendamentoFerriCT/Tasks/
git add SistemaWebAgendamentoFerriCT/Migrations/202605172019119_AddMercadoPagoFields*
git add SistemaWebAgendamentoFerriCT/Views/Agendamento/Pagamento.cshtml
git add SistemaWebAgendamentoFerriCT/Views/Agendamento/Retorno.cshtml
git add SistemaWebAgendamentoFerriCT/Views/Admin/RegistrarPagamentoManual.cshtml
git add .gitignore

git status   # confira que Web.secrets.config NÃO está sendo commitado
```

Sugestão de mensagem:

```
feat: integra Mercado Pago Checkout Pro (PIX + Débito) com defesas anti-fraude

- MercadoPagoService cria Preference e consulta Payment via API oficial
- PagamentoController.Webhook valida HMAC-SHA256 em constant time
- Idempotência de webhook via WebhookEventoId UNIQUE filtrado
- Re-busca de payment antes de confirmar (não confia em payload)
- Validação de transaction_amount contra valor server-side
- Job de cleanup automático: cancela agendamentos pendentes >1h
- Admin pode registrar pagamento manual no balcão (CodigoTransacao=MANUAL-{Guid})
- Cleanup de tentativas anteriores ao recriar Preference
- Guard de 1 PendentePagamento por cliente
- Ownership check em todos os endpoints de pagamento

Falta apenas teste end-to-end no sandbox (Task #9 do plano).
Ver HANDOFF.md para detalhes e próximos passos.
```

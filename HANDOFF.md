# Handoff — Integração Mercado Pago

**Branch atual:** `feat/mercado-pago-integration`
**Última sessão:** 2026-05-19
**Status:** Débito validado end-to-end no sandbox. PIX adiado pra validação em produção.

## Para retomar na próxima sessão

```powershell
git checkout feat/mercado-pago-integration
```

E diz pro Claude: "continua do HANDOFF.md".

---

## ✅ Onde paramos (2026-05-19)

Sessão de hoje fechou os bloqueios da sessão anterior e validou o golden path:

1. **Webhook funcionando** — pagamento aprovado no MP → webhook chega → status do agendamento muda pra `Confirmado` no dashboard ✅
2. **Erro Razor em `Retorno.cshtml` corrigido** — `@if` interno de bloco `else { }` removido (commit `66d0b52`)
3. **Métodos de pagamento filtrados** — agora bloqueia `prepaid_card` e `digital_currency` (commit `66d0b52`)

### O que faltou na Task #9

Cenários ainda não exercitados no sandbox:

- [ ] Cartão recusado (CVV inválido) — 2min
- [ ] Webhook forjado (POST sem `x-signature`) — 5min, **segurança importante**
- [ ] Pagamento manual admin — 3min
- [ ] Ownership check (cliente A acessa agendamento B) — 5min, **segurança importante**
- [ ] Timeout 1h (cleanup job) — espera longa, logs em VS Output
- [ ] PIX — adiado pra validar em produção (sandbox MP quebrado, ver abaixo)

---

## 🔑 Aprendizados críticos desta sessão (LEIA antes de retomar)

### 1. Test users do MP — fluxo correto

O MP migrou (faz uns 2 anos) de tokens `TEST-*` direto na conta real para o modelo de **test users**:

- Cria 2 test users no painel developers: **seller** (vendedor) e **buyer** (comprador)
- Faz **logout** da conta real do MP
- Faz **login com email/senha do test seller**
- Dentro da conta dele, cria/abre uma **aplicação**
- AccessToken e PublicKey aparecem como `APP_USR-...` (sandbox porque a conta é de teste)
- Esse `APP_USR-...` é o que vai no `Web.secrets.config`

⚠️ NÃO confundir: `APP_USR-*` da conta REAL = produção (cobra dinheiro). `APP_USR-*` do test seller = sandbox.

### 2. WebhookSecret é separado do AccessToken

Quando você cadastra o webhook no painel MP, o painel gera uma **chave secreta** específica do webhook. Ela:

- **NÃO é** o AccessToken
- Tem que ser copiada do painel → Sua aplicação → Webhooks → notificação cadastrada → **Chave secreta** (botão "Mostrar")
- Vai em `MercadoPago:WebhookSecret` no `Web.secrets.config`

Se você recadastra o webhook (ou trocou de aplicação), o MP **gera uma chave secreta nova**. Tem que atualizar o `Web.secrets.config` ou todos os webhooks vão retornar 401.

**Sintoma**: painel MP → Webhooks → notificações mostram **"401 - Com erro"** em vermelho.

### 3. Simulação de webhook do painel sempre dá 500

Não é bug nosso. O simulador do MP manda `data.id: "123456"` (ID falso). Nosso código valida HMAC (passa ✅), depois tenta buscar o pagamento na API real do MP, que retorna 404 → 500.

**Como saber se HMAC funcionou pela simulação:** olha o Output do VS. Se aparecer `Webhook MP: falha ao consultar payment 123456` → HMAC OK. Se aparecer `assinatura inválida` → WebhookSecret errado.

### 4. PIX em sandbox do MP é quebrado pra test sellers

Cadastro de chave PIX no painel do test seller retorna erro genérico `PKF03-VYL8CGTZPHCP`. Confirmado depois de várias tentativas com tipos diferentes de chave (aleatória/email/CPF).

**Decisão tomada:** validar PIX só em produção, com transação de **R$ 0,50–1,00**, depois do deploy. A conta real do MP já tem PIX habilitado.

### 5. account_money não dá pra excluir via API

MP retorna 400 "account_money cannot be excluded" se a gente tentar. O comprador vê **"Saldo em conta"** se tiver saldo na carteira MP. Em produção, raramente é problema (comprador comum não tem saldo). Aceitável.

---

## ⚙️ Estado do código

### Mudanças commitadas hoje (`66d0b52`)

**`Views/Agendamento/Retorno.cshtml`** (linha 197):
```csharp
// Antes
@if (autoRefresh)  // ❌ erro: já estamos em contexto C# (dentro de else { })

// Depois
if (autoRefresh)   // ✅ sem @, porque já estamos em código
```

**`MercadoPago/MercadoPagoService.cs`** (~linha 86):
```csharp
excluded_payment_types = new[]
{
    new { id = "credit_card" },
    new { id = "prepaid_card" },     // NOVO — bloqueia cartões pré-pagos
    new { id = "ticket" },
    new { id = "atm" },
    new { id = "digital_currency" }  // NOVO — bloqueia cripto
}
```

### Web.secrets.config (não versionado)

Estado funcional confirmado hoje:
```xml
<add key="MercadoPago:AccessToken" value="APP_USR-..." />     <!-- test seller -->
<add key="MercadoPago:PublicKey" value="APP_USR-..." />       <!-- test seller -->
<add key="MercadoPago:WebhookSecret" value="..." />            <!-- chave secreta do webhook -->
<add key="MercadoPago:NotificationUrl" value="https://<tunel>/Pagamento/Webhook" />
<add key="MercadoPago:BackUrlBase" value="https://<tunel>" />
```

---

## 🔁 Como retomar testes na próxima sessão

### Pré-requisitos

1. **Subir túnel Cloudflare**
   ```powershell
   $env:GODEBUG = "netdns=cgo"
   & "$env:USERPROFILE\cloudflared\cloudflared.exe" tunnel --url https://localhost:44358 --no-tls-verify --http-host-header localhost:44358
   ```

2. **A URL do túnel muda a cada execução.** Atualize:
   - `Web.secrets.config` → `NotificationUrl` e `BackUrlBase`
   - Painel MP do test seller → Webhooks → editar URL pra novo túnel

3. **Build + F5** (Visual Studio)

### Cartão de teste pra Débito

- Aprovado: `5031 4332 1540 6351`, CVV `123`, validade futura, nome `APRO`
- Recusado: mesmo cartão, nome `OTHE` (ou CVV inválido)

### Comprador

Logar no checkout do MP com o **test buyer** (não a conta real). Email/senha estão no arquivo local `SistemaWebAgendamentoFerriCT/contas teste MP.txt` (gitignored).

### Cenários sugeridos (em ordem de prioridade)

1. **Webhook forjado** (segurança):
   ```powershell
   Invoke-WebRequest -Uri "https://<tunel>/Pagamento/Webhook" -Method POST -Body '{"type":"payment","data":{"id":"123"}}' -ContentType "application/json"
   ```
   Esperado: HTTP 401 "Invalid signature"

2. **Ownership check** (segurança): tentar acessar `/Agendamento/Pagamento/{idDeOutroCliente}` logado como cliente X → esperado 403

3. **Cartão recusado**: cartão de teste com nome `OTHE` → status `Cancelado`

4. **Pagamento manual admin**: `/Admin/RegistrarPagamentoManual/{id}` → forma=Dinheiro → status `Confirmado`

5. **Timeout 1h**: cria agendamento sem pagar, espera, vê no Output `AgendamentoCleanupJob` cancelando

---

## 📦 Tasks concluídas (visão geral)

| # | Task | Status |
|---|---|---|
| 1 | Setup MP + túnel HTTPS | ✅ |
| 2 | Migration `AddMercadoPagoFields` | ✅ |
| 3 | `MercadoPagoService` | ✅ |
| 4 | Refactor `AgendamentoController` | ✅ |
| 5 | Webhook + validação HMAC | ✅ |
| 6 | Página de retorno | ✅ (bug Razor corrigido hoje) |
| 7 | Job de cleanup automático | ✅ |
| 8 | Pagamento manual admin | ✅ |
| 9 | **Teste end-to-end no sandbox** | 🟡 Débito validado, outros cenários pendentes |

---

## 🚧 Pontos de atenção / dívidas técnicas

1. **`SenhaAdminHash` hardcoded** em `AdminController` (SHA-256 de "123"). Em produção, mover para `Web.secrets.config` e usar bcrypt/Argon2.

2. **`FormaPagamento` em `Pagamento` ainda é `[Required]`** mas usamos placeholder `"Aguardando"`. Aceitável; poderia virar nullable.

3. **`MapearFormaPagamento`** não trata `prepaid_card` — retorna `"Aguardando"`. Como agora bloqueamos prepaid via Preference, isso só importa se algum gateway escapar. Baixa prioridade.

4. **`AgendamentoCleanupJob` usa `Timer` estático** — em IIS com app pool recycle agressivo pode perder ticks. Considerar Hangfire para produção.

5. **Sem logging estruturado** — usando `System.Diagnostics.Trace`. NLog/Serilog em produção.

6. **Sem testes automatizados** — prioridade alta: `WebhookSignatureValidator` (HMAC).

7. **URL do túnel Cloudflare muda** — em produção haverá domínio fixo. Em dev, lembrar de atualizar webhook + secrets a cada execução.

8. **PIX em produção não foi validado ainda** — depois do deploy, fazer uma transação real de R$ 0,50–1,00 pra confirmar que o fluxo funciona.

9. **Sandbox MP às vezes demora** alguns segundos pra webhook chegar. A página `Retorno.cshtml` tem auto-refresh de 5s pra cobrir isso.

---

## 💰 Defesas de segurança implementadas

| # | Defesa | Local |
|---|---|---|
| 1 | Access Token em `Web.secrets.config` gitignored | `Web.config` + `.gitignore` |
| 2 | HMAC-SHA256 do `x-signature` em **constant time** | `WebhookSignatureValidator.cs` |
| 3 | Tolerância de timestamp 5min (anti-replay) | `WebhookSignatureValidator.MaxClockSkew` |
| 4 | `WebhookEventoId` UNIQUE filtrado (idempotência) | Migration + `PagamentoController.Webhook` |
| 5 | `CodigoTransacao` UNIQUE filtrado (anti-replay de paymentId) | Migration |
| 6 | `PreferenceId` UNIQUE filtrado | Migration |
| 7 | Re-busca via `/v1/payments/{id}` antes de confirmar | `PagamentoController.Webhook` step 5 |
| 8 | Validação `transaction_amount` == valor server-side | `PagamentoController.Webhook` step 7 |
| 9 | `X-Idempotency-Key` na criação de Preference | `MercadoPagoService.CriarPreferenceAsync` |
| 10 | `excluded_payment_types`: bloqueia crédito/prepaid/boleto/ATM/cripto | `MercadoPagoService.CriarPreferenceAsync` |
| 11 | Ownership check (cliente A não acessa B) → 403 | `AgendamentoController.Pagamento`/`IniciarPagamento`/`Retorno` |
| 12 | Guard "1 PendentePagamento por cliente" | `AgendamentoController.Create` POST |
| 13 | Pagamentos antigos cancelados ao recriar Preference | `AgendamentoController.IniciarPagamento` |
| 14 | Botão `disabled` ao submeter (anti duplo-click) | `Pagamento.cshtml` JS |
| 15 | Webhook **sem** `[ValidateAntiForgeryToken]` (defesa = HMAC) | `PagamentoController.Webhook` |
| 16 | Valor recalculado server-side em todo lugar | controllers |
| 17 | `[FiltroAcesso]` herdado em `RegistrarPagamentoManual` | `AdminController` |
| 18 | Forma de pagamento manual via whitelist | `AdminController.RegistrarPagamentoManual` POST |
| 19 | CPF sanitizado (só dígitos) antes de mandar pro MP | `MercadoPagoService.SomenteDigitos` |
| 20 | TLS 1.2 forçado no HttpClient | `MercadoPagoService.CriarHttpClient` |

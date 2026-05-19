# Roteiro — Apresentação da Integração Mercado Pago

Passo a passo para demonstrar a integração ao professor. **Imprima ou abra em outra janela durante a apresentação.**

---

## ⏱️ Antes de começar (faça 15min antes da aula)

### 1. Subir o túnel Cloudflare

PowerShell:
```powershell
$env:GODEBUG = "netdns=cgo"
& "$env:USERPROFILE\cloudflared\cloudflared.exe" tunnel --url https://localhost:44358 --no-tls-verify --http-host-header localhost:44358
```

**Copie a URL pública** que aparecer (ex: `https://xxx-xxx-xxx.trycloudflare.com`).

### 2. Atualizar `Web.secrets.config` se a URL mudou

Caminho: `SistemaWebAgendamentoFerriCT/Web.secrets.config`

```xml
<add key="MercadoPago:NotificationUrl" value="https://<URL-NOVA>/Pagamento/Webhook" />
<add key="MercadoPago:BackUrlBase" value="https://<URL-NOVA>" />
```

### 3. Atualizar webhook no painel do MP se a URL mudou

- Logar em `mercadopago.com.br` com **test seller** (email/senha no arquivo `contas teste MP.txt`)
- Ir em `developers/panel` → sua aplicação → **Webhooks**
- Editar URL para `https://<URL-NOVA>/Pagamento/Webhook`
- Eventos: **Pagamentos** (marcar)

### 4. Build + F5 no Visual Studio

`Ctrl+Shift+B` para build → `F5` para rodar.

### 5. Abrir as abas que vai usar

- **Aba 1**: `https://localhost:44358` — sua aplicação
- **Aba 2** (anônima): `https://localhost:44358` — pra entrar como cliente
- **Aba 3**: painel MP do test seller → **Webhooks** (pra mostrar os webhooks chegando)
- **Aba 4** (Visual Studio): janela **Output** aberta (View → Output → Show output from: Debug)

---

## 🎬 Roteiro da apresentação (15–20min)

### Parte 1: Contexto e Arquitetura (3min)

**Diga:** *"O sistema é um sistema de agendamento da academia Ferri CT. Quando o cliente agenda uma aula, ele precisa pagar pela vaga. Integrei o Mercado Pago Checkout Pro para isso, permitindo pagamento via Débito e PIX."*

**Mostre no quadro/slide um fluxo simples:**

```
Cliente               Nossa App              Mercado Pago
   │                     │                         │
   │ Agenda aula         │                         │
   ├────────────────────>│                         │
   │                     │ Cria Preference (API)   │
   │                     ├────────────────────────>│
   │                     │ Retorna init_point      │
   │                     │<────────────────────────┤
   │ Redireciona p/ MP   │                         │
   │<────────────────────┤                         │
   │ Paga no MP          │                         │
   ├──────────────────────────────────────────────>│
   │                     │ Webhook (HMAC assinado) │
   │                     │<────────────────────────┤
   │                     │ Valida HMAC,            │
   │                     │ atualiza status         │
   │ Volta para a app    │                         │
   │<────────────────────┤                         │
```

### Parte 2: Demo prática (8min)

#### 2.1 Mostrar o agendamento

1. **Aba anônima**: abrir `https://localhost:44358`
2. Logar como cliente (usar conta existente)
3. Ir em **Agendar** → escolher uma aula → confirmar
4. Resultado: agendamento criado com status **PendentePagamento**

**Diga:** *"O agendamento já está reservado, mas a vaga só fica confirmada quando o pagamento é aprovado."*

#### 2.2 Iniciar pagamento

1. Clicar no botão **"Pagar com Mercado Pago R$ 50,00"**
2. Vai redirecionar pro Checkout Pro do MP

**Diga:** *"Aqui o sistema criou uma `Preference` na API do MP usando o `AccessToken`, e o MP retornou uma URL única do checkout. Note que excluí cartão de crédito, boleto, ATM, cripto e cartões pré-pagos via `excluded_payment_types` — só Débito e PIX são aceitos."*

#### 2.3 Pagar no Checkout do MP

1. Na tela do MP, logar com **test buyer** (email/senha no `contas teste MP.txt`)
2. Escolher **Cartão de Débito**
3. Usar cartão de teste:
   - Número: `5031 4332 1540 6351`
   - CVV: `123`
   - Validade: qualquer data futura (ex: `12/30`)
   - Nome do titular: `APRO` (importante — esse nome força aprovação no sandbox)
4. Confirmar pagamento → MP mostra **"Pronto! Seu pagamento foi aprovado"**

#### 2.4 Mostrar o webhook chegando (parte mais importante)

1. **Volte pra Aba 3** (painel MP → Webhooks)
2. Atualizar a página — vai aparecer um novo evento `payment.created` com status **"200 - Entregue"** (verde)

**Diga:** *"O MP enviou uma notificação assinada pro meu servidor. O sistema validou a assinatura HMAC-SHA256, buscou os dados reais do pagamento na API do MP (pra não confiar no payload), validou o valor contra o que o servidor calculou, e marcou o agendamento como Confirmado."*

3. **Aba 4** (VS Output): mostrar que **não tem erros** no log.

#### 2.5 Mostrar o status atualizado

1. Voltar pra **Aba anônima** (a página de retorno fez auto-refresh)
2. Status agora: **Confirmado** ✅
3. Ir no perfil do cliente — mostrar que o agendamento aparece como confirmado

---

### Parte 3: Defesas de segurança (5min — parte crítica)

**Abra essas tabelas/slides:**

| Ataque possível | Defesa implementada | Onde está no código |
|---|---|---|
| **Webhook forjado** (alguém manda POST falso pro endpoint) | HMAC-SHA256 com `x-signature` em **constant time** (resistente a timing attacks) | `MercadoPago/WebhookSignatureValidator.cs` |
| **Replay attack** (capturar webhook antigo e reenviar) | Tolerância de timestamp de 5min + `WebhookEventoId` UNIQUE no banco | `WebhookSignatureValidator.MaxClockSkew` + `Migrations/202605172019119_AddMercadoPagoFields.cs` |
| **Tampering de valor** (cliente edita HTML/JS e manda valor menor) | Valor sempre recalculado no servidor; `transaction_amount` do webhook é comparado com `ValorExperimental`/`ValorMatricula` | `PagamentoController.Webhook` step 7 |
| **Confiar em payload do webhook** | Após validar HMAC, sistema **re-busca** o pagamento via `/v1/payments/{id}` antes de confirmar | `PagamentoController.Webhook` step 5 |
| **Webhook duplicado** (MP reenvia) | Idempotência por `WebhookEventoId` UNIQUE — segundo POST retorna "Already processed" | `PagamentoController.Webhook` step 4 |
| **Pagamento duplicado** (cliente abre 2 abas) | Guard "1 PendentePagamento por cliente"; ao reiniciar pagamento, cancela tentativas antigas | `AgendamentoController.Create` POST + `IniciarPagamento` |
| **Cliente A acessar pagamento de B** | Ownership check em todos os endpoints → 403 Forbidden | `AgendamentoController.Pagamento`/`IniciarPagamento`/`Retorno` |
| **Roubo de credenciais** | `AccessToken` em `Web.secrets.config` **gitignored** — nunca no repositório | `.gitignore` + `Web.config` |
| **Downgrade de TLS** | Força TLS 1.2 no HttpClient | `MercadoPagoService.CriarHttpClient` |

**Mostre os 2 arquivos chave:**

1. **`MercadoPago/WebhookSignatureValidator.cs`** — destacar:
   - Linha do `ConstantTimeEquals` — *"compara em tempo constante pra não vazar prefixos corretos via timing"*
   - `MaxClockSkew = 5 minutos` — *"se alguém capturar o webhook e reenviar 6min depois, rejeito"*

2. **`Controllers/PagamentoController.Webhook`** — destacar as 9 etapas comentadas:
   ```
   1. Extrai headers + body
   2. Valida HMAC
   3. Filtra tipo de evento
   4. Verifica idempotência
   5. Re-busca payment na API do MP
   6. Valida external_reference
   7. Valida valor (anti-tampering)
   8. Localiza Pagamento alvo
   9. Atualiza status
   ```

---

### Parte 4: Recursos extras (2–3min se sobrar tempo)

#### Job de limpeza automática

**Mostrar `Tasks/AgendamentoCleanupJob.cs`**

*"Se o cliente não pagar em 1 hora, um job de background cancela o agendamento e libera a vaga pra outro cliente."*

#### Pagamento manual pelo admin

*"O admin pode registrar pagamentos feitos no balcão (dinheiro, PIX direto, débito na maquininha) — gera um `CodigoTransacao` no formato `MANUAL-{Guid}` pra rastreamento."*

Mostrar: **`/Admin/RegistrarPagamentoManual/{id}`**

---

## 🎤 Perguntas prováveis e respostas

| Pergunta | Resposta |
|---|---|
| *"E se o MP cair na hora do pagamento?"* | O cliente é redirecionado pra página de retorno com `status=failure` e pode tentar novamente. Se o webhook nunca chegar, o cleanup job cancela em 1h e libera a vaga. |
| *"E se o valor do agendamento mudar entre a criação e o pagamento?"* | O valor vai dentro da `Preference` no MP. Mesmo se eu mudar no banco depois, o webhook compara com `ValorExperimental`/`ValorMatricula` definidos como `const` no controller — qualquer divergência é rejeitada. |
| *"Por que escolheu Checkout Pro e não Checkout Transparente?"* | Checkout Pro é hospedado pelo MP — não tenho responsabilidade PCI sobre dados de cartão. Reduz drasticamente a superfície de ataque e o escopo de compliance. Checkout Transparente exigiria tokenização no front e SAQ-A-EP no PCI DSS. |
| *"E PIX?"* | PIX está implementado no código (não excluído na `Preference`). No ambiente de sandbox do MP, o cadastro de chave PIX em contas de teste está com bug conhecido (erro `PKF03`), então a validação completa do fluxo PIX só vai acontecer em produção com transação real de baixo valor. |
| *"Por que não usar o SDK oficial do MP em vez de HttpClient direto?"* | O SDK do MP pra .NET tem suporte limitado pra .NET Framework 4.x (esse projeto é MVC 5). Faria mais sentido em .NET 6+. Como precisava só de 2 endpoints (`/checkout/preferences` e `/v1/payments/{id}`), implementação direta com `HttpClient` ficou mais simples e sem dependências. |
| *"E o Idempotency-Key?"* | Toda chamada de criação de `Preference` manda `X-Idempotency-Key` único por agendamento. Se a chamada cair no meio (timeout, retry), MP retorna a mesma `Preference` em vez de criar duplicata. |
| *"Como teste isso tudo?"* | Testes manuais no sandbox do MP com cartões de teste documentados (`APRO` aprova, `OTHE` recusa). Pra automatizar, o próximo passo seria escrever testes unitários do `WebhookSignatureValidator` — é onde bugs de HMAC machucam mais. |
| *"Se eu mudar o `WebhookSecret` no `.config`, o que acontece?"* | Todos os webhooks vão falhar com 401. O MP vai retentar (sim, ele retenta automaticamente). Quando eu corrigir, o cleanup job de 1h evita que agendamentos fiquem travados indefinidamente. |

---

## 📂 Arquivos pra ter abertos no Visual Studio

Em ordem de importância pra mostrar:

1. **`Controllers/PagamentoController.cs`** — endpoint do webhook, 9 etapas comentadas
2. **`MercadoPago/WebhookSignatureValidator.cs`** — HMAC + constant time
3. **`MercadoPago/MercadoPagoService.cs`** — criação de Preference + consulta de payment
4. **`Controllers/AgendamentoController.cs`** — método `IniciarPagamento`
5. **`Models/Pagamento.cs`** — campos `PreferenceId`, `WebhookEventoId`, `CodigoTransacao`
6. **`Migrations/202605172019119_AddMercadoPagoFields.cs`** — índices UNIQUE filtrados
7. **`Web.config`** — mostrar que `AccessToken` vem de arquivo externo gitignored
8. **`Tasks/AgendamentoCleanupJob.cs`** — job de limpeza

---

## 🚨 Plano B se algo der errado na hora

| Problema | Solução rápida |
|---|---|
| Túnel Cloudflare caiu | Reabrir o terminal e rodar o comando de novo. **Atenção: URL muda.** Atualizar `Web.secrets.config` + webhook no painel MP. |
| Webhook retornando 401 | `WebhookSecret` errado. Copiar de novo do painel MP → Webhooks → notificação → Chave secreta. Restart F5. |
| Erro ao criar Preference | Olhar Output do VS — provavelmente o `AccessToken` é da conta errada (real em vez de test seller) ou expirou. |
| Página de retorno não atualiza | Auto-refresh é a cada 5s. Se mesmo assim não atualizar, é o webhook que não chegou — conferir painel MP. |
| Cartão recusado sem motivo | Verificar nome do titular: `APRO` aprova, `OTHE` recusa, qualquer outro = comportamento aleatório do sandbox. |
| Demo trava em "Pendente" | Não esperar mais de 30s. Mostrar diretamente o painel do MP com o webhook (`200 - Entregue`) e o log no VS, pra provar que a integração funcionou e é só o front que não atualizou. |

---

## ✅ Checklist final antes de começar a apresentação

- [ ] Túnel Cloudflare rodando e URL anotada
- [ ] `Web.secrets.config` com URL atualizada
- [ ] Webhook do MP apontando pra URL atual
- [ ] Visual Studio rodando (F5) sem erros de build
- [ ] Janela **Output** do VS visível
- [ ] 4 abas do navegador prontas (app, anônima, painel MP, código)
- [ ] Cartão de teste anotado: `5031 4332 1540 6351` / CVV `123` / Nome `APRO`
- [ ] Email/senha do test buyer em mãos
- [ ] HANDOFF.md aberto pra consulta rápida se travar

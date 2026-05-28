# Handoff — Sistema Ferri CT

Documento de contexto operacional para Claude e para o usuário. Pensado pra **reta final de projeto** (demo 03/06/2026). Histórico de sessões vive no `git log` + commits — aqui só fica o que tem **utilidade contínua**.

---

## 🎯 Estado atual

- **Branch ativa:** `feat/mercado-pago-integration`
- **Demo:** 03/06/2026 (PC do professor, `git clone` + F5)
- **Integração MP:** end-to-end validada em sandbox (Saldo em conta funcional, 2026-05-24)
- **Uncommitted intencional** (21 arquivos `M`): Etapa 5c+5d aplicada — `--text-muted #5A5A65→#8A8A95` nos 2 layouts + `font-size` mínimo subido pra 0.72rem em 19 views + 2 layouts. BOM UTF-8 confirmado em todos. Aguardando smoke test visual do usuário (F5) antes de commitar.
- **Próximo passo:** após smoke test OK → commitar 5c+5d → seguir com Etapa 3 (CSS externo) ou Etapa 5a+5b (ARIA) ou Etapa 2 (hero home). Plano completo em `C:\Users\User\.claude\plans\d-user-downloads-requisitos-do-professor-curried-bird.md`.

Para reabrir contexto numa nova sessão, basta dizer ao Claude: **"continua do HANDOFF.md"** ou apontar a etapa do plano.

---

## 🚀 Como rodar o projeto

### Cenário 1: dev local sem MP real
```powershell
# Visual Studio: F5 (StartAction = Project → abre Home/Index)
```
Banco LocalDB sobe via migration automática no `Application_Start`. Seed cria 2 clientes demo, professores e turmas.

### Cenário 2: dev local com MP funcional (webhook)
```powershell
.\start-demo.ps1   # sobe ngrok com subdomínio reservado + abre VS
# Em seguida: F5 no VS
```
Script já inclui `--host-header=rewrite` (sem isso → IIS Express retorna 400 Invalid Hostname). URL fixa: `https://molehill-salvation-clothes.ngrok-free.dev`.

### Credenciais de demo

| Tipo | Login | Senha |
|---|---|---|
| Admin | (URL `/Admin/Login`) | `123` (SHA-256 hardcoded) |
| Cliente demo | `demo@ferrict.com.br` | CPF `529.982.247-25` |
| Cliente 2 | (no seed) | CPF `111.444.777-35` |

### URLs principais
- `/` → Home (público)
- `/Cliente/Login`, `/Cliente/Cadastro`, `/Cliente/Perfil`
- `/Agendamento/Create`, `/Agendamento/Pagamento/{id}`, `/Agendamento/Retorno/{id}`
- `/Admin/Login`, `/Admin/Index`, `/Admin/Agendamentos`, `/Admin/Alunos`

---

## 📁 Estrutura do projeto

Stack: **ASP.NET MVC 5 + EF 6 Code First + SQL Server LocalDB + Razor**. Detalhes em `CLAUDE.md` — abaixo só o que ele não cobre:

- **`App_Start/RouteConfig.cs`** — rota default `Home/Index`
- **`App_Start/BundleConfig.cs`** — existe mas hoje subutilizado; CSS vem inline nos `.cshtml` + Bootstrap via CDN no `_Layout`
- **`Tasks/AgendamentoCleanupJob.cs`** — timer estático iniciado em `Global.asax`. Cancela pendentes > 1h
- **`Services/MercadoPagoService.cs`** — `CriarPreferenceAsync`, `BuscarPaymentAsync`. TLS 1.2 forçado
- **`Services/WebhookSignatureValidator.cs`** — HMAC-SHA256 constant-time + tolerância 5min
- **`Web.config`** — credenciais TEST do MP commitadas (justificativa em comentário no próprio arquivo); `Web.secrets.config` continua gitignored como rede de segurança
- **`.csproj.user`** — não trackeado pelo git; `<StartAction>Project</StartAction>` é local
- **`start-demo.ps1`** + **`README.md`** + **`DEMONSTRACAO-MERCADO-PAGO.md`** — onboarding pro professor

---

## ⚠️ Regras críticas pra editar este projeto

Erros silenciosos que custam tempo se ignorados.

### Razor `@@` em CSS dentro de `.cshtml`
Toda diretiva CSS com `@` precisa de `@@`: `@@media`, `@@keyframes`, `@@font-face`. Sem isso, o Razor tenta interpretar como C# e o CSS é **silenciosamente quebrado**. Não dá warning de compilação.

### UTF-8 com BOM obrigatório em `.cshtml`
ASP.NET MVC 5 deste projeto interpreta arquivo sem BOM como Windows-1252 → mojibake nos acentos (`HORÁRIO` vira `HORÃ¡RIO`, em-dash vira `ã€"`).
- `Edit`/`replace_all` **preserva** o BOM existente — seguro
- `Write` (reescrita completa) grava **sem** BOM — quebra. Após `Write` em `.cshtml` com acentos, regravar:
  ```powershell
  $path = "Views/.../Arquivo.cshtml"
  $content = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
  $utf8Bom = New-Object System.Text.UTF8Encoding($true)
  [System.IO.File]::WriteAllText($path, $content, $utf8Bom)
  ```
- Verificar BOM: `[System.IO.File]::ReadAllBytes($path)[0..2]` deve ser `239 187 191`

### Mudança em `Web.config`
IIS Express recicla automaticamente — não precisa reiniciar VS, mas a primeira request pós-edit é lenta (recompila views).

### Mudança em modelo EF
Sempre `Add-Migration NomeMigration` no Package Manager Console **antes** de F5. O `MigrateDatabaseToLatestVersion<>` no `Global.asax` aplica na inicialização. **NUNCA** usar `DropCreateDatabaseIfModelChanges` (quebra em produção).

### Inicializador do banco
Em `Global.asax.cs`: `SetInitializer(new MigrateDatabaseToLatestVersion<SistemaContext, Configuration>())`. Não trocar.

### Bug recorrente: `media (max-width)` sem `@@`
Já apareceu uma vez no `CriarAluno.cshtml` — não era `@media` sem `@@`, era literalmente `media` sem prefixo nenhum. Sempre dá grep antes de fechar trabalho em CSS.

---

## 🔄 Workflow de sessão

### Pré-sessão (Claude lê primeiro)
1. `git status` — saber o que está uncommitted
2. `git log --oneline -10` — últimos commits
3. `HANDOFF.md` (este arquivo) — contexto operacional
4. Plano ativo em `~/.claude/plans/` se mencionado pelo usuário
5. `MEMORY.md` é carregado automaticamente — não precisa ler

### Pós-sessão (encerramento)
1. Confirmar `git status` limpo (ou registrar uncommitted no HANDOFF se intencional)
2. Atualizar **só** a seção "Estado atual" deste arquivo — não inflar com histórico de sessão
3. Se houve aprendizado novo durável → salvar como memória, não como bullet no HANDOFF
4. Se plano avançou → atualizar plano (`~/.claude/plans/...`) com etapas concluídas
5. Perguntar antes de `git push` (afeta shared state — não é local)

### Quando o usuário pede pra "encerrar a sessão"
- Commits limpos, working tree limpo
- Estado atual + próximo passo escritos no HANDOFF
- Não pushar sem confirmação

---

## 💳 Lições críticas MP/ngrok (preservar — evita debug repetido)

### Test sellers do MP
- `APP_USR-*` da conta REAL = produção. `APP_USR-*` do test seller = sandbox. **Não confundir**.
- Pra gerar test users: painel developers MP → criar 2 (seller + buyer) → logout da conta real → login com test seller → criar aplicação → AccessToken/PublicKey aparecem como `APP_USR-...`

### WebhookSecret ≠ AccessToken
Cada webhook cadastrado no painel gera chave secreta própria. Vai em `MercadoPago:WebhookSecret` no `Web.config`. Recadastrar webhook → nova chave → atualizar ou todos retornam 401.

### Simulador de webhook do painel sempre dá 500
Manda `data.id: "123456"` (ID falso). HMAC passa, busca real retorna 404 → 500. **Não é bug.** Como saber se HMAC funcionou: log diz `Webhook MP: falha ao consultar payment 123456` (HMAC OK). Se aparecer `assinatura inválida` → WebhookSecret errado.

### ngrok grátis interstitial
Primeira visita ao domínio em cada sessão de browser mostra "Visit Site". Webhooks server-to-server NÃO passam por essa tela. **Pro dia da demo:** abrir a URL ngrok no navegador do professor 5min antes e clicar Visit Site.

### Test buyer só mostra meios salvos
Tela do MP em sandbox não dá "Cartão novo". Caminho que funciona pra demo: **Saldo em conta** (R$ 142,50 inicial, gastos R$ 50/transação). Cartão Débito Mastercard `5031 4332 1540 6351` está cadastrado mas rejeita por divergência de bandeira.

### account_money não dá pra excluir
MP retorna 400 "account_money cannot be excluded". Comprador sempre vê "Saldo em conta" se tiver — aceitável.

### PIX em sandbox quebrado pra test sellers
Cadastro de chave PIX retorna `PKF03-VYL8CGTZPHCP`. Validar PIX só em produção com R$ 0,50-1,00.

### Cartões de teste pra Débito (se decidir não usar Saldo em conta)
- Aprovado: `5031 4332 1540 6351`, CVV `123`, validade futura, nome `APRO`
- Recusado: mesmo cartão, nome `OTHE`

### CPF em comparação exata
`ClienteController.Login` faz `c.CPF == vm.CPF` sem sanitizar. A view tem máscara JS formatando `529.982.247-25`. Seed precisa ter CPF formatado (já está).

---

## 🚧 Dívidas técnicas conhecidas

Lista mantida pra não esquecer — nenhuma bloqueia a demo.

1. **`SenhaAdminHash` hardcoded** (SHA-256 de "123") em `AdminController`. Em produção: secrets externos + bcrypt/Argon2
2. **`FormaPagamento` é `[Required]`** mas usamos placeholder `"Aguardando"` — aceitável
3. **`MapearFormaPagamento`** não trata `prepaid_card` — retorna `"Aguardando"`. Baixa prioridade (bloqueamos via Preference)
4. **`AgendamentoCleanupJob` usa Timer estático** — IIS app pool recycle pode perder ticks. Em produção: Hangfire
5. **Sem logging estruturado** — `System.Diagnostics.Trace`. Em produção: NLog/Serilog
6. **Sem testes automatizados** — prioridade ao `WebhookSignatureValidator` (HMAC) quando der
7. **PIX em produção não validado** — sandbox quebrado
8. **Credenciais TEST commitadas no Web.config** — decisão deliberada pra demo acadêmica; em produção real voltar pra `Web.secrets.config` ou Azure Key Vault

---

## 🛡️ Defesas de segurança implementadas

Lista preservada — útil se professor perguntar sobre segurança da integração.

| # | Defesa | Local |
|---|---|---|
| 1 | HMAC-SHA256 do `x-signature` em constant time | `WebhookSignatureValidator.cs` |
| 2 | Tolerância de timestamp 5min (anti-replay) | `WebhookSignatureValidator.MaxClockSkew` |
| 3 | `WebhookEventoId` UNIQUE filtrado (idempotência) | Migration + `PagamentoController.Webhook` |
| 4 | `CodigoTransacao` UNIQUE filtrado (anti-replay paymentId) | Migration |
| 5 | `PreferenceId` UNIQUE filtrado | Migration |
| 6 | Re-busca via `/v1/payments/{id}` antes de confirmar | `PagamentoController.Webhook` step 5 |
| 7 | Validação `transaction_amount` == valor server-side | `PagamentoController.Webhook` step 7 |
| 8 | `X-Idempotency-Key` na criação de Preference | `MercadoPagoService.CriarPreferenceAsync` |
| 9 | `excluded_payment_types`: bloqueia crédito/prepaid/boleto/ATM/cripto | `MercadoPagoService.CriarPreferenceAsync` |
| 10 | Ownership check (cliente A não acessa B) → 403 | `AgendamentoController.Pagamento/IniciarPagamento/Retorno` |
| 11 | Guard "1 PendentePagamento por cliente" | `AgendamentoController.Create` POST |
| 12 | Pagamentos antigos cancelados ao recriar Preference | `AgendamentoController.IniciarPagamento` |
| 13 | Botão `disabled` ao submeter (anti duplo-click) | `Pagamento.cshtml` JS |
| 14 | Webhook **sem** `[ValidateAntiForgeryToken]` (defesa = HMAC) | `PagamentoController.Webhook` |
| 15 | Valor recalculado server-side sempre | controllers |
| 16 | `[FiltroAcesso]` herdado em `RegistrarPagamentoManual` | `AdminController` |
| 17 | Forma de pagamento manual via whitelist | `AdminController.RegistrarPagamentoManual` POST |
| 18 | CPF sanitizado antes de mandar pro MP | `MercadoPagoService.SomenteDigitos` |
| 19 | TLS 1.2 forçado no HttpClient | `MercadoPagoService.CriarHttpClient` |
| 20 | Credenciais MP só sandbox (sem dinheiro real) | `Web.config` (comentário com justificativa) |

---

## 🔍 Auditoria branch `demo/sem-mp-simulado` (2026-05-27)

Punch list dos pontos falhos identificados na branch demo (commit `bac18fd`). Detalhes completos em `memory/audit-demo-branch-2026-05-27.md` (só nesta máquina). Snapshot — verificar contra código atual antes de implementar.

### 🔴 Crítico (quebra fluxo na demo)
1. **`Views/Admin/ListaEspera.cshtml` não existe** — `AdminController.ListaEspera()` (linha 487) retorna `View(espera)` mas o `.cshtml` não foi criado. Dashboard tem 2 links pra `/Admin/ListaEspera` (`Admin/Index.cshtml` linhas 479 e 565) → clicar quebra com `InvalidOperationException`.
2. **Admin não tem promoção de lista de espera** — sem view e sem action pra `AguardandoVaga → PendentePagamento`. Regra de negócio do CLAUDE.md diz "admin promove" mas não há mecanismo.

### 🟠 Alto (UX quebrada / ação órfã)
3. **"Registrar Pagamento Manual" inacessível pela UI** — action + view existem mas nem `Agendamentos.cshtml` (linhas 449-458) nem `DetalhesAgendamento.cshtml` (linhas 340-343) linkam. Admin teria que adivinhar URL `/Admin/RegistrarPagamentoManual/{id}`.
4. **Card mobile de Alunos sem botão "Ver detalhes"** — tabela tem 3 botões (Ver/Editar/Excluir), cards (linhas 487-495 de `Alunos.cshtml`) só têm Editar/Excluir. Mobile esconde tabela → detalhes inacessíveis no celular.
5. **Cliente não consegue cancelar agendamento** — não há `CancelarAgendamento` no `AgendamentoController` nem no `ClienteController`. Cliente fica preso até cleanup de 1h (e só pra `PendentePagamento`).

### 🟡 Médio (segurança / regra de negócio)
6. **`AgendamentoController.Confirmacao(int id)` sem ownership check** (linha 435) — qualquer cliente logado vê confirmação de qualquer outro. `Pagamento` e `Retorno` têm; `Confirmacao` esqueceu.
7. **`AgendamentoController.ListaEspera(int id)` (cliente) sem ownership check** (linha 461) — mesmo problema do #6.
8. **`AdminController.EditarAgendamento` POST sem whitelist de status** (linha 318) — `agendamento.Status = status` aceita qualquer string. POST forjado pode setar `"Lalala"` e quebrar máquina de estados.
9. **`AguardandoVaga` não bloqueia novo agendamento** — guard "1 pendente por cliente" (linha 215) só checa `PendentePagamento`/`EmAnalise`. Cliente em lista de espera pode criar outros agendamentos.

### 🟢 Baixo (cosmético / código morto)
10. **`Alunos.cshtml:376` "Novos este mês"** compara só `.Month`, ignora ano. Janeiro/2024 conta em janeiro/2026.
11. **`EditarAgendamento` GET dropdown** (linha 302) só tem 3 status. Falta `AguardandoVaga` e `EmAnalise`.
12. **Copy "Mercado Pago" em `RegistrarPagamentoManual.cshtml:151`** — trocar pra "fora do sistema digital" nessa branch demo.
13. **Código morto** — `MercadoPago/MercadoPagoService.cs`, `MercadoPago/IMercadoPagoService.cs`, `PagamentoController.cs` (webhook) não são chamados nessa branch. Não atrapalham mas sujam.

### Ordem sugerida pra implementação
1. #1 (criar `ListaEspera.cshtml`)
2. #3 (botão "Registrar Pagamento Manual" em DetalhesAgendamento)
3. #4 (botão "Ver detalhes" no card mobile)
4. #5 (cancelamento pelo cliente)
5. #6/#7 (ownership check — 1 linha cada)
6. #8 (whitelist no EditarAgendamento)

#2, #9-#13 são extras se sobrar tempo.

---

## 📌 Plano de requisitos do professor

Arquivo completo: `C:\Users\User\.claude\plans\d-user-downloads-requisitos-do-professor-curried-bird.md`

**Etapas:**
1. ✅ Etapa 1 — consolidar uncommitted (P1 + P2 + Perfil + footer + F5 fix) — concluída 2026-05-27
2. 🟠 Etapa 2 — hero da home no mobile (`Views/Home/Index.cshtml`)
3. 🟠 Etapa 3 — CSS externo (escopo médio: design system + 5 views-chave)
4. 🟠 Etapa 4 — `@media print` global + dedicado em `Agendamento/Retorno`
5. 🟡 Etapa 5 — Acessibilidade mínima (`aria-label`, `aria-hidden`, normalizar font-size, contraste `--text-muted`) — **5c (font-size ≥0.72rem) e 5d (contraste `--text-muted`) aplicadas 2026-05-27, uncommitted aguardando smoke test.** 5a+5b (ARIA) pendentes.
6. 🟢 Etapa 6 — Re-validar PC externo na véspera 02/06

Cenários de teste pendentes (não bloqueiam demo, fazer se sobrar tempo):
- [ ] Cartão recusado (nome `OTHE`)
- [ ] Webhook forjado (POST sem `x-signature` → 401)
- [ ] Pagamento manual admin
- [ ] Ownership check (cliente A acessa B → 403)
- [ ] Timeout 1h cleanup

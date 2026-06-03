# SistemaWebAgendamentoFerriCT

Sistema web de agendamento da academia **Ferri CT** (Presidente Prudente/SP). ASP.NET MVC 5 + Entity Framework 6 + SQL Server.

## Stack

- ASP.NET MVC 5 (.NET Framework)
- Entity Framework 6 — Code First com Migrations
- SQL Server (LocalDB em dev)
- Razor Views (`.cshtml`)
- Bootstrap-like CSS custom

## Estrutura

- `Controllers/` — `HomeController`, `AdminController`, `ClienteController`, `AgendamentoController`
- `Models/` — entidades EF + `SistemaContext` (em `Data/`)
- `Views/` — Razor views organizadas por controller
- `Migrations/` — EF migrations + `Configuration.cs` com seed completo
- `Data/SistemaContext.cs` — DbContext

## Inicialização do banco

- `Global.asax.cs` usa `MigrateDatabaseToLatestVersion<SistemaContext, Configuration>` (não usar `DropCreateDatabaseIfModelChanges` nem `CreateDatabaseIfNotExists` — isso quebra em produção).
- Seed completo vive em `Migrations/Configuration.cs` com `AddOrUpdate` (idempotente). Inclui Professores, Turmas e HorariosTurma.
- Para banco zerado em máquina nova: basta rodar a aplicação. Migrations + seed rodam automaticamente.

## Convenções Razor (IMPORTANTE)

Em arquivos `.cshtml`, **toda** diretiva CSS que começa com `@` precisa ser escapada com `@@`:

```css
@@media (max-width: 768px) { ... }
@@keyframes fadeUp { ... }
```

Sem o `@@`, o Razor tenta interpretar como código C# e o CSS é silenciosamente quebrado (elementos com `opacity: 0` ficam invisíveis, grids não colapsam no mobile, etc).

## Autenticação

- Login do cliente: SHA-256 + token de sessão. Session key: `ClienteId`, `ClienteNome`.
- Admin: rota separada com guard `[Authorize]`.

## Regras de negócio principais

### Agendamento

- Academia **fechada aos domingos** e em feriados (fixos + móveis via algoritmo de Páscoa).
- Cliente só pode ter 1 agendamento ativo por (data, horário).
- Aula **Experimental** é exclusiva para clientes sem agendamento prévio (`ClienteJaAgendou`).
- Turmas não têm capacidade máxima — não há lista de espera (decisão de 2026-05-29 nesta branch demo).

### Pagamento (integração Mercado Pago em andamento)

Ver `memory/payment-rules.md` (memória pessoal de Claude) ou o resumo abaixo:

- Gateway: **Mercado Pago Checkout Pro**
- Métodos: **PIX + Débito apenas** (sem crédito, sem boleto)
- Timeout de pendente: **1h** → cancela e libera vaga
- Sem reembolso após pagamento aprovado
- Pagamento manual pelo admin permitido (CodigoTransacao = "MANUAL-{Guid}")
- Máximo 1 agendamento `PendentePagamento` por cliente

### Estados de Agendamento

`PendentePagamento` → `EmAnalise` → `Confirmado` / `Cancelado`
`PendentePagamento` → `Confirmado` (PIX direto)

## Segurança

- Todo POST com `[ValidateAntiForgeryToken]` exceto webhooks externos (que validam por HMAC).
- Valores monetários **sempre recalculados no servidor** — nunca confiar em valor vindo do form.
- Access Tokens e secrets do MP **nunca** no source control. Vão em `Web.config` (com `Web.config.local` fora do git).
- Webhook do MP: validação HMAC-SHA256 do header `x-signature` em constant time + tolerância de timestamp 5min.

## Comandos úteis

```powershell
# Restaurar pacotes NuGet
nuget restore

# Atualizar banco (no Package Manager Console do Visual Studio)
Update-Database

# Criar nova migration
Add-Migration NomeDaMigration
```

## Convenções de commit

Mensagens em português, prefixo conventional commits (`fix:`, `feat:`, `refactor:`, etc).
Co-autoria com Claude permitida via trailer `Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>`.
